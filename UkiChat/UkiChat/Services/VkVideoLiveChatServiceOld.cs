using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace UkiChat.Services;

/// <summary>
///     Сервис для работы с чатом VK Video Live через WebSocket с Protobuf протоколом
/// </summary>
public class VkVideoLiveChatServiceOld : IVkVideoLiveChatServiceOld, IDisposable
{
    private readonly IVkVideoLiveApiService _apiService;
    private CancellationTokenSource? _cancellationTokenSource;
    private string? _chatChannel;
    private ClientWebSocket? _webSocket;


    public VkVideoLiveChatServiceOld(IVkVideoLiveApiService apiService)
    {
        _apiService = apiService;
    }


    public void Dispose()
    {
        _webSocket?.Dispose();
        _cancellationTokenSource?.Dispose();
    }

    public async Task ConnectAsync(string accessToken, string chatChannel)
    {
        try
        {
            // Убираем префикс "api-" из названия канала если он есть
            _chatChannel = chatChannel.Replace("api-channel-", "channel-");


            Console.WriteLine($"[VkVideoLiveChat] Используем канал: {_chatChannel} (оригинал: {chatChannel})");

            // Получаем WebSocket токен через API
            Console.WriteLine("[VkVideoLiveChat] Получение WebSocket токена...");
            var wsTokenResponse = await _apiService.GetWebSocketTokenAsync(accessToken);
            var wsToken = wsTokenResponse.Data.Token;
            Console.WriteLine(
                $"[VkVideoLiveChat] WebSocket токен получен: {wsToken.Substring(0, Math.Min(10, wsToken.Length))}...");

            // Создаем WebSocket клиент
            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();

            // Добавляем заголовки
            _webSocket.Options.SetRequestHeader("Origin", "https://vkvideo.ru");
            _webSocket.Options.SetRequestHeader("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");


            // Даем время на обработку подключения
            await Task.Delay(1000);


            // Даем время на обработку подписки
            await Task.Delay(500);
        }
        catch (Exception ex)
        {
        }
    }

    public Task DisconnectAsync()
    {
        throw new NotImplementedException();
    }

    public event EventHandler<ChatMessageEventArgs>? MessageReceived;
    public event EventHandler? Connected;
    public event EventHandler<DisconnectEventArgs>? Disconnected;
    public event EventHandler<ErrorEventArgs>? Error;
}