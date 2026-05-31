using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UkiChat.Model.Chat.EventArgs;
using ErrorEventArgs = UkiChat.Model.Chat.EventArgs.ErrorEventArgs;

namespace UkiChat.Model.YouTube;

/// <summary>
///     Клиент YouTube live-чата на внутреннем InnerTube API (без OAuth и квот).
///     Поток: канал → live videoId → страница live_chat (apiKey + continuation) →
///     polling youtubei/v1/live_chat/get_live_chat по continuation-токенам.
/// </summary>
public class YouTubeChatClient : IDisposable
{
    private const string BaseUrl = "https://www.youtube.com";

    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    // YouTube рекомендует timeoutMs (часто 5-10с). Ограничиваем сверху, чтобы чат был отзывчивее.
    private const int MaxPollIntervalMs = 2000;

    // Извлечение параметров InnerTube и стартового continuation со страницы live_chat
    private static readonly Regex ApiKeyRegex = new("\"INNERTUBE_API_KEY\":\"([^\"]+)\"", RegexOptions.Compiled);

    private static readonly Regex ClientVersionRegex =
        new("\"INNERTUBE_CONTEXT_CLIENT_VERSION\":\"([^\"]+)\"", RegexOptions.Compiled);

    private static readonly Regex ContinuationRegex = new(
        "\"(?:invalidationContinuationData|timedContinuationData|reloadContinuationData)\":\\{[^}]*?\"continuation\":\"([^\"]+)\"",
        RegexOptions.Compiled);

    private static readonly Regex CanonicalVideoIdRegex = new(
        "<link rel=\"canonical\" href=\"https://www\\.youtube\\.com/watch\\?v=([^\"]+)\"", RegexOptions.Compiled);

    private readonly HttpClient _httpClient;
    private readonly ILogger<YouTubeChatClient> _logger;

    private CancellationTokenSource? _cts;
    private bool _disposed;

    // Дедуп: стартовый continuation после reconnect повторяет бэклог сообщений
    private readonly HashSet<string> _seenIds = [];

    private string _apiKey = "";
    private string _clientVersion = "";
    private string _continuation = "";
    private string _videoId = "";

    public YouTubeChatClient(ILogger<YouTubeChatClient> logger, HttpClient? httpClient = null)
    {
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _cts?.Cancel();
        _cts?.Dispose();
        _httpClient.Dispose();
        _disposed = true;
    }

    public event EventHandler<YouTubeChatMessageEventArgs>? MessageReceived;

    /// <summary>Удаление сообщения модератором. Аргумент — id удалённого сообщения.</summary>
    public event EventHandler<string>? MessageDeleted;

    public event EventHandler? Connected;
    public event EventHandler<DisconnectEventArgs>? Disconnected;
    public event EventHandler<ErrorEventArgs>? Error;

    /// <summary>
    ///     Резолвит активную трансляцию канала в videoId через /live-редирект.
    ///     channel: "@handle", "handle", "UCxxxx" (channelId) или полный URL.
    /// </summary>
    public async Task<string?> ResolveLiveVideoIdAsync(string channel, CancellationToken cancellationToken = default)
    {
        var liveUrl = BuildLiveUrl(channel);
        _logger.LogInformation("Резолвим live-видео канала: {Url}", liveUrl);

        var html = await _httpClient.GetStringAsync(liveUrl, cancellationToken);
        var videoId = CanonicalVideoIdRegex.Match(html).Groups[1].Value;
        if (string.IsNullOrEmpty(videoId))
        {
            _logger.LogWarning("Не удалось определить live videoId для '{Channel}' (канал не в эфире?)", channel);
            return null;
        }

        _logger.LogInformation("Канал '{Channel}' в эфире: videoId={VideoId}", channel, videoId);
        return videoId;
    }

    /// <summary>Подключается к чату канала: резолвит live-видео и запускает polling.</summary>
    public async Task ConnectByChannelAsync(string channel, CancellationToken cancellationToken = default)
    {
        var videoId = await ResolveLiveVideoIdAsync(channel, cancellationToken);
        if (string.IsNullOrEmpty(videoId))
            throw new InvalidOperationException($"Канал '{channel}' сейчас не в эфире");

        await ConnectByVideoIdAsync(videoId, cancellationToken);
    }

    /// <summary>Подключается к чату конкретной трансляции по её videoId.</summary>
    public async Task ConnectByVideoIdAsync(string videoId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Останавливаем предыдущий цикл polling, если он ещё жив (переподключение)
            _cts?.Cancel();
            _cts?.Dispose();
            _seenIds.Clear();

            await InitChatAsync(videoId, cancellationToken);

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Connected?.Invoke(this, System.EventArgs.Empty);
            _ = Task.Run(() => PollLoopAsync(_cts.Token), _cts.Token);
        }
        catch (Exception ex)
        {
            OnError($"Ошибка подключения к чату: {ex.Message}", ex);
            throw;
        }
    }

    public Task DisconnectAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        OnDisconnected("Disconnect requested");
        return Task.CompletedTask;
    }

    /// <summary>Загружает страницу live_chat и достаёт apiKey, версию клиента и стартовый continuation.</summary>
    private async Task InitChatAsync(string videoId, CancellationToken cancellationToken)
    {
        _videoId = videoId;
        var url = $"{BaseUrl}/live_chat?is_popout=1&v={videoId}";
        var html = await _httpClient.GetStringAsync(url, cancellationToken);

        _apiKey = ApiKeyRegex.Match(html).Groups[1].Value;
        _clientVersion = ClientVersionRegex.Match(html).Groups[1].Value;
        _continuation = ContinuationRegex.Match(html).Groups[1].Value;

        if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_continuation))
            throw new InvalidOperationException(
                "Чат недоступен: не найден continuation (трансляция завершена или чат отключён)");

        _logger.LogInformation("Чат инициализирован: videoId={VideoId} clientVersion={Version}", videoId, _clientVersion);
    }

    /// <summary>Цикл polling: POST get_live_chat, разбор actions, ожидание timeoutMs, новый continuation.</summary>
    private async Task PollLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            int timeoutMs;
            try
            {
                timeoutMs = await PollOnceAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                OnError($"Ошибка polling: {ex.Message}", ex);
                timeoutMs = 5000;
            }

            try
            {
                await Task.Delay(Math.Min(timeoutMs, MaxPollIntervalMs), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>Один цикл опроса. Возвращает рекомендованную паузу до следующего опроса (мс).</summary>
    private async Task<int> PollOnceAsync(CancellationToken cancellationToken)
    {
        var endpoint = $"{BaseUrl}/youtubei/v1/live_chat/get_live_chat?key={_apiKey}&prettyPrint=false";
        var payload = new
        {
            context = new { client = new { clientName = "WEB", clientVersion = _clientVersion, hl = "en", gl = "US" } },
            continuation = _continuation
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("continuationContents", out var cc) ||
            !cc.TryGetProperty("liveChatContinuation", out var lcc))
        {
            // Чат завершился — продолжения нет
            OnDisconnected("Live chat ended");
            _cts?.Cancel();
            return 0;
        }

        // Следующий continuation + timeoutMs
        var timeoutMs = 1000;
        if (lcc.TryGetProperty("continuations", out var conts) && conts.GetArrayLength() > 0)
        {
            var cont = conts[0];
            foreach (var prop in cont.EnumerateObject())
            {
                var data = prop.Value;
                if (data.TryGetProperty("continuation", out var nextCont))
                    _continuation = nextCont.GetString() ?? _continuation;
                if (data.TryGetProperty("timeoutMs", out var t))
                    timeoutMs = t.GetInt32();
            }
        }

        if (lcc.TryGetProperty("actions", out var actions))
            foreach (var action in actions.EnumerateArray())
                ProcessAction(action);

        return timeoutMs;
    }

    private void ProcessAction(JsonElement action)
    {
        if (action.TryGetProperty("addChatItemAction", out var add) &&
            add.TryGetProperty("item", out var item))
        {
            if (item.TryGetProperty("liveChatTextMessageRenderer", out var textRenderer))
                EmitMessage(ParseRenderer(textRenderer, isSuperChat: false));
            else if (item.TryGetProperty("liveChatPaidMessageRenderer", out var paidRenderer))
                EmitMessage(ParseRenderer(paidRenderer, isSuperChat: true));
            else if (item.TryGetProperty("liveChatPaidStickerRenderer", out var stickerRenderer))
                EmitMessage(ParsePaidSticker(stickerRenderer));
            else if (item.TryGetProperty("liveChatMembershipItemRenderer", out var memberRenderer))
                EmitMessage(ParseMembership(memberRenderer));
            return;
        }

        // Удаление одного сообщения модератором (markChatItemAsDeletedAction / removeChatItemAction)
        if ((action.TryGetProperty("markChatItemAsDeletedAction", out var del) &&
             del.TryGetProperty("targetItemId", out var delId)) ||
            (action.TryGetProperty("removeChatItemAction", out del) &&
             del.TryGetProperty("targetItemId", out delId)))
        {
            var id = delId.GetString();
            if (!string.IsNullOrEmpty(id))
                MessageDeleted?.Invoke(this, id);
        }
    }

    private YouTubeChatMessage ParseRenderer(JsonElement r, bool isSuperChat)
    {
        string? amountText = null;
        if (isSuperChat && r.TryGetProperty("purchaseAmountText", out var amount) &&
            amount.TryGetProperty("simpleText", out var amountSimple))
            amountText = amountSimple.GetString();

        return BuildMessage(r, ParseRuns(r), isSuperChat, amountText);
    }

    /// <summary>Супер-стикер (liveChatPaidStickerRenderer): сумма + картинка стикера, текста нет.</summary>
    private YouTubeChatMessage ParsePaidSticker(JsonElement r)
    {
        var parts = new List<YouTubeChatPart>();
        if (r.TryGetProperty("sticker", out var sticker))
        {
            var stickerUrl = LastThumbnailUrl(sticker);
            if (!string.IsNullOrEmpty(stickerUrl))
                parts.Add(new YouTubeChatPart(YouTubeChatPartKind.Emote, stickerUrl));
        }

        string? amountText = null;
        if (r.TryGetProperty("purchaseAmountText", out var amount) &&
            amount.TryGetProperty("simpleText", out var amountSimple))
            amountText = amountSimple.GetString();

        return BuildMessage(r, parts, isSuperChat: true, amountText);
    }

    /// <summary>
    ///     Спонсорство (liveChatMembershipItemRenderer): новый участник или веха ("Участник уже 6 мес.").
    ///     Текст шапки кладём в AmountText, чтобы оформить как платную поддержку (зелёный хайлайт).
    /// </summary>
    private YouTubeChatMessage ParseMembership(JsonElement r)
    {
        // headerPrimaryText — для вех, headerSubtext — для новых участников
        var label = TextOf(r, "headerPrimaryText");
        if (string.IsNullOrEmpty(label)) label = TextOf(r, "headerSubtext");

        return BuildMessage(r, ParseRuns(r), isSuperChat: true, string.IsNullOrEmpty(label) ? null : label);
    }

    private YouTubeChatMessage BuildMessage(JsonElement r, List<YouTubeChatPart> parts, bool isSuperChat,
        string? amountText)
    {
        return new YouTubeChatMessage
        {
            Id = r.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
            AuthorName = r.TryGetProperty("authorName", out var an) && an.TryGetProperty("simpleText", out var ans)
                ? ans.GetString() ?? ""
                : "",
            AuthorChannelId = r.TryGetProperty("authorExternalChannelId", out var ac) ? ac.GetString() ?? "" : "",
            AuthorPhotoUrl = r.TryGetProperty("authorPhoto", out var ap) ? LastThumbnailUrl(ap) : "",
            Parts = parts,
            Badges = ParseBadges(r),
            IsSuperChat = isSuperChat,
            AmountText = amountText,
            TimestampUsec = r.TryGetProperty("timestampUsec", out var ts) && long.TryParse(ts.GetString(), out var v)
                ? v
                : 0
        };
    }

    /// <summary>Разбирает message.runs в части (текст + эмодзи).</summary>
    private static List<YouTubeChatPart> ParseRuns(JsonElement r)
    {
        var parts = new List<YouTubeChatPart>();
        if (!r.TryGetProperty("message", out var message) || !message.TryGetProperty("runs", out var runs))
            return parts;

        foreach (var run in runs.EnumerateArray())
        {
            if (run.TryGetProperty("text", out var text))
                parts.Add(new YouTubeChatPart(YouTubeChatPartKind.Text, text.GetString() ?? ""));
            else if (run.TryGetProperty("emoji", out var emoji))
            {
                // Стандартные unicode-эмодзи (isCustomEmoji отсутствует) отдаём символом из emojiId,
                // кастомные эмодзи канала — картинкой.
                var isCustom = emoji.TryGetProperty("isCustomEmoji", out var ic) && ic.GetBoolean();
                if (!isCustom && emoji.TryGetProperty("emojiId", out var eid) &&
                    eid.GetString() is { Length: > 0 } symbol)
                {
                    parts.Add(new YouTubeChatPart(YouTubeChatPartKind.Text, symbol));
                    continue;
                }

                var emoteUrl = LastThumbnailUrl(emoji.TryGetProperty("image", out var img) ? img : default);
                if (!string.IsNullOrEmpty(emoteUrl))
                    parts.Add(new YouTubeChatPart(YouTubeChatPartKind.Emote, emoteUrl));
            }
        }

        return parts;
    }

    private static List<YouTubeChatBadge> ParseBadges(JsonElement r)
    {
        var badges = new List<YouTubeChatBadge>();
        if (!r.TryGetProperty("authorBadges", out var authorBadges))
            return badges;

        foreach (var b in authorBadges.EnumerateArray())
        {
            if (!b.TryGetProperty("liveChatAuthorBadgeRenderer", out var br)) continue;
            var tooltip = br.TryGetProperty("tooltip", out var tt) ? tt.GetString() ?? "" : "";
            if (br.TryGetProperty("customThumbnail", out var ct))
                badges.Add(new YouTubeChatBadge(null, LastThumbnailUrl(ct), tooltip));
            else if (br.TryGetProperty("icon", out var icon) && icon.TryGetProperty("iconType", out var it))
                badges.Add(new YouTubeChatBadge(it.GetString(), null, tooltip));
        }

        return badges;
    }

    /// <summary>Достаёт текст из поля, которое может быть simpleText или { runs: [...] }.</summary>
    private static string TextOf(JsonElement r, string propertyName)
    {
        if (!r.TryGetProperty(propertyName, out var el)) return "";
        if (el.TryGetProperty("simpleText", out var simple)) return simple.GetString() ?? "";
        if (!el.TryGetProperty("runs", out var runs)) return "";

        var sb = new StringBuilder();
        foreach (var run in runs.EnumerateArray())
            if (run.TryGetProperty("text", out var t))
                sb.Append(t.GetString());
        return sb.ToString();
    }

    /// <summary>Берёт URL последней (самой крупной) миниатюры из объекта с массивом thumbnails.</summary>
    private static string LastThumbnailUrl(JsonElement container)
    {
        if (container.ValueKind != JsonValueKind.Object ||
            !container.TryGetProperty("thumbnails", out var thumbs) ||
            thumbs.ValueKind != JsonValueKind.Array || thumbs.GetArrayLength() == 0)
            return "";

        var last = thumbs[thumbs.GetArrayLength() - 1];
        if (!last.TryGetProperty("url", out var url)) return "";
        var u = url.GetString() ?? "";
        // Часть картинок (стикеры) приходит с протокол-относительным URL "//..."
        return u.StartsWith("//", StringComparison.Ordinal) ? "https:" + u : u;
    }

    private static string BuildLiveUrl(string channel)
    {
        channel = channel.Trim();
        if (channel.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return channel.TrimEnd('/') + "/live";
        if (channel.StartsWith("UC", StringComparison.Ordinal) && channel.Length == 24)
            return $"{BaseUrl}/channel/{channel}/live";
        var handle = channel.StartsWith('@') ? channel : "@" + channel;
        return $"{BaseUrl}/{handle}/live";
    }

    private void EmitMessage(YouTubeChatMessage message)
    {
        // Пропускаем повторы (бэклог после reconnect). Кэш ограничиваем, чтобы не рос бесконечно.
        if (!string.IsNullOrEmpty(message.Id))
        {
            if (!_seenIds.Add(message.Id))
                return;
            if (_seenIds.Count > 5000)
                _seenIds.Clear();
        }

        MessageReceived?.Invoke(this, new YouTubeChatMessageEventArgs { Message = message, VideoId = _videoId });
    }

    private void OnDisconnected(string reason)
    {
        Disconnected?.Invoke(this, new DisconnectEventArgs { Reason = reason, ChannelName = _videoId });
    }

    private void OnError(string message, Exception? exception = null)
    {
        _logger.LogError(exception, "{Message}", message);
        Error?.Invoke(this, new ErrorEventArgs { Message = message, Exception = exception });
    }
}
