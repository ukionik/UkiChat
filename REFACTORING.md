# План рефакторинга UkiChat

Составлен 04.08.2026 на основе разбора кодовой базы (бэкенд ~12.6k строк C#, фронтенд ~2k строк
собственных компонентов). Порядок фаз выбран так, чтобы каждая следующая работала с уже
сокращённым кодом.

## Главный риск: рефакторить сейчас нечем подстраховаться

36 тестов в `UkiChat.Tests` — почти все интеграционные: `SevenTvApiTest`, `TwitchApiTest`,
`VkVideoLiveApiTest`, `YouTubeApiServiceTest` ходят в живые API и требуют сети и токенов.
Чистых юнит-тестов на логику — только `TwitchEmoteIndexNormalizerTest` и `TomlTest`.

При этом в коде десятки комментариев вида «раньше здесь было X, и чат намертво вставал»:
`TwitchChatService.cs:43-48` (поколения клиента), `SignalRService.cs:16-27` (очередь на клиента),
`VkVideoLiveChatService.cs:236-239` (фильтр `OperationCanceledException` по токену).
Это выстраданные исправления гонок — слепой рефакторинг их вернёт.

---

## Фаза 0. Сетка безопасности — ГОТОВО (04.08.2026)

**Отдача:** обязательна для остального · **Риск:** нет · **Факт:** 93 юнит-теста, 46 мс, без сети

Сделано:

- Тесты разделены на unit и integration: 8 сетевых классов помечены
  `[Trait(TestCategories.Category, TestCategories.Integration)]` (`TestCategories.cs`).
  Из 118 тестов 25 интеграционных, 93 юнит.
- `InternalsVisibleTo("UkiChat.Tests")` в `UkiChat.csproj`; функции разбора в `UkiChatMessage`
  (`ParseTextWithLinks`, `ParseTextWithThirdPartyEmotes`, `FormatDonationAmount`,
  `UnescapeIrcTagValue`) переведены из `private` в `internal`. Поведение не менялось.
- Новые характеризующие тесты:
  - `UkiChatMessageParsingTest` — разбор на текст/ссылку/эмоут, включая раскладку пробелов,
    приоритет ссылки над эмоутом, регистрозависимость эмоутов, форматирование суммы доната
  - `UkiChatMessageMentionTest` — границы слова в упоминаниях, экранирование ника,
    неприкосновенность не-Normal типов
  - `ColorUtilTest`, `TwitchWatchStreakTest`

Команды:

```bash
dotnet test -p:SkipWebBuild=true --filter "Category!=Integration"   # быстрые, без сети
dotnet test -p:SkipWebBuild=true --filter "Category=Integration"    # требуют сети и токенов
```

`-p:SkipWebBuild=true` пропускает сборку Nuxt — она не нужна для тестов и занимает минуты.

### Три бага, найденных попутно — ИСПРАВЛЕНЫ (04.08.2026)

1. **Цвет ника не переживал перезапуск.** `ColorUtil.GetDisplayNameColor` брал индекс палитры
   из `displayName.GetHashCode()`, а хэш строки в .NET рандомизируется при каждом старте процесса:
   у зрителя без своего цвета Twitch цвет менялся после каждого запуска приложения.
   Заменено на FNV-1a поверх UTF-8 байт имени в нижнем регистре. Приведение регистра даёт один
   цвет одному человеку, даже если площадки пишут ник по-разному.

   Заодно `GetVkVideoLiveNickColor` получил сигнатуру `(int? colorIndex, string displayName)`:
   индекс от VK — выбор площадки и остаётся в приоритете, но при его отсутствии цвет теперь
   считается по нику, а не отдаётся первым цветом палитры VK всем подряд. Итог: если площадка
   цвет не прислала, зритель выглядит одинаково в Twitch, YouTube и VK.

   **Проверено отдельно:** reduction через `% 15` даёт ровное распределение (на 500 ников
   26–37 в корзине при ожидаемых 33). Вариант с умножением и сдвигом старших бит оказался
   заметно хуже (макс 84) — у FNV-1a лучше перемешаны младшие биты, поэтому оставлен модуль.
   Зашитые в тест цвета посчитаны независимой реализацией, сверенной с каноническими
   контрольными векторами FNV-1a.

2. **Разворот escape-последовательностей IRC шёл в неверном порядке.** Цепочка `Replace`
   обрабатывала `\\` последней, поэтому вход `\\s` (экранированный слэш + буква s) превращался
   в слэш с пробелом вместо `\s`. Логика вынесена в `Utils/IrcTagUtil.Unescape` и переписана
   одним проходом слева направо. Тот же баг был во второй копии — `TwitchWatchStreak`
   разворачивал только `\s` голым `Replace`; теперь обе точки используют общую функцию.

3. **Ник, оканчивающийся не на буквоцифру, не срабатывал в упоминаниях никогда** — шаблон
   заканчивался на `\b`. Заменено на просмотр вперёд `(?![\p{L}\p{N}_])`, симметричный левому.
   Правая граница при этом сохранена: `c++` внутри `c++builder` упоминанием не считается.

---

## Фаза 1. Эмоут-провайдеры — самая большая копипаста (~600 строк → ~200)

**Отдача:** −400 строк, 4 слоя × 3 → 1 · **Риск:** низкий · **Оценка:** 1-2 дня

7TV / FFZ / BTTV продублированы на четырёх слоях:

| Слой | Файлы | Отличия |
|---|---|---|
| API-сервис | `SevenTvApiService` (123), `FfzApiService` (130), `BttvApiService` (118) | только URL и парсинг JSON |
| Memory-репозиторий | `SevenTvEmotesRepository`, `FfzEmotesRepository`, `BttvEmotesRepository` (~45 строк) | **только generic-параметр** |
| DB-репозиторий | `SevenTvEmoteRepository`, `FfzEmoteRepository`, `BttvEmoteRepository` | только имя коллекции |
| Загрузчики | 6 методов в `TwitchChatService.cs:732-1109` | идентичная схема «БД → API → сохранить в БД» |

Шаги:

1. Общий тип `Emote(string Id, string Name, string Url)` вместо трёх записей-близнецов.
2. `EmotesRepository<T>` в `Repositories/Memory` — три класса схлопываются в один generic.
3. `EmoteRepository<TEntity>(LiteDatabase db, string collection)` для БД.
   **Важно:** в `SevenTvEmoteRepository.cs:35-40` живёт нетривиальное исправление
   (проставление `Channel` при сохранении) — оно должно переехать в базовый класс, а не потеряться.
4. Единый `IEmoteProvider` с `GetGlobalEmotesAsync()` / `GetChannelEmotesAsync(id)`;
   три реализации остаются, но HttpClient-обвязка (заголовки, таймаут, `DiagnosticHttpHandler`)
   выносится в базовый класс.
5. Один метод `LoadEmotesAsync(provider, scope)` вместо шести в `TwitchChatService`.

---

## Фаза 2. Пять реализаций «цикла повторов» → одна

**Отдача:** −250 строк, 5 копий → 1 · **Риск:** средний · **Оценка:** 1 день

Одна и та же семантика написана заново пять раз:

- `TwitchChatService.ReconnectLoopAsync` (382-410) + `BadgesRetryLoopAsync` (840-882) + `IdleWatchdogLoopAsync`
- `VkVideoLiveChatService.StartReconnectLoop/CancelReconnectLoop/ReconnectLoopAsync` (177-266)
- `YouTubeChatService.StartReconnectLoop/CancelReconnectLoop/ReconnectLoopAsync` (129-205) —
  посимвольно совпадает с VK, кроме интервала и тела попытки
- `AppInitializationService.TwitchApiRetryLoopAsync` (159-193)

Извлечь `RetryLoop` / `ReconnectSupervisor` с параметрами: интервал/бэкофф, максимум попыток,
тело попытки, токен отмены.

**Обязательно сохранить два тонких места, которые уже стоили багов:**
- фильтр `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)`
- освобождение CTS в `finally`

---

## Фаза 3. Разобрать `TwitchChatService` (1160 строк, 13 зависимостей)

**Отдача:** читаемость главного файла · **Риск:** высокий · **Оценка:** 2-3 дня

Делает четыре разные работы. Разделить по границам, которые уже видны в коде:

| Новый класс | Что забирает |
|---|---|
| `TwitchChatConnection` | `BuildClient`, `WireClientEvents`, watchdog, реконнект, поколения клиента, учётные данные |
| `TwitchEventTranslator` | 12 обработчиков событий → `UkiChatMessage`, `TierLabel`, де-дуп массовых подарков |
| `TwitchAssetsLoader` | значки, эмоуты, награды, `EnsureBroadcasterIdAsync` |
| `TwitchChatService` | тонкая координация: `ConnectAsync`, `ChangeChannelAsync`, `ReapplyCredentialsAsync` |

Самый рискованный кусок из-за логики поколений — делать **после** Фаз 1 и 2, строго по одному
классу за коммит.

---

## Фаза 4. Мелкие, но частые дубли

**Отдача:** −200 строк · **Риск:** низкий · **Оценка:** 1 день

1. **Счётчики зрителей** — `TwitchViewerCountService` (79), `VkVideoLiveViewerCountService` (66),
   `YouTubeViewerCountService` (62) отличаются тремя строчками.
   → `ViewerCountServiceBase` с абстрактным `PollAsync`, наследники по ~15 строк.
2. **Репозитории настроек** — `TwitchSettingsRepository`, `VkVideoLiveSettingsRepository`,
   `YouTubeSettingsRepository`, `DonationAlertsSettingsRepository`: одинаковый
   `Include(...).Include(...).FindOne(x => x.AppSettings.Profile.Active)`.
   → `ActiveProfileSettingsRepository<T>`.
3. **`DatabaseService`** (202 строки) — 20+ методов вида «прочитать AppSettings → изменить одно
   поле → сохранить». → хелпер `MutateAppSettings(Action<AppSettings>)`.
4. **`IChatService<T>` протекает** — `VkVideoLiveChatService.LoadGlobalDataAsync/LoadChannelDataAsync`
   бросают `NotImplementedException` (строки 175-183). Заряженное ружьё: вызов через интерфейс
   уронит инициализацию. Вынести загрузку данных в отдельный опциональный интерфейс.

---

## Фаза 5. Три системы логирования → одна

**Отдача:** диагностируемость · **Риск:** низкий · **Оценка:** 0.5-1 день

Параллельно живут: `StartupDiagnostics` (191 вызов), `ILogger` (157),
`Console.WriteLine` (13, в том числе в бою — `VkVideoLiveChatService`, все три эмоут-сервиса).
Одно и то же событие часто пишется дважды в разные файлы.

- Минимальный шаг: убрать `Console.WriteLine` из продакшн-кода.
- Дальше: либо сделать `StartupDiagnostics` тонким сахаром поверх `ILogger` с отдельным
  Serilog-синком, либо явно зафиксировать границу — `StartupDiagnostics` только для
  startup-таймингов, `ILogger` для всего остального.

---

## Фаза 6. `AppHub` — service locator и обёртки

**Отдача:** тестируемость · **Риск:** низкий · **Оценка:** 0.5 дня

- 12 полей вида `ContainerLocator.Container.Resolve<IXxx>()` (`AppHub.cs:18-29`).
  SignalR умеет обычный DI через конструктор — хаб станет тестируемым.
- `Measure(...)` / `MeasureResult(...)` навешаны вручную на каждый метод →
  заменить на `IHubFilter`, минус ~60 строк шаблона.
- Симметричные тройки `ChangeXChannel` / `UpdateXSettings` по платформам просятся в один
  параметризованный метод — вкусовщина, трогать в последнюю очередь.

---

## Что трогать НЕ надо

- **Темы фронтенда** — 30 компонентов, но всего 1998 строк (~66 на файл), и общее уже вынесено
  в `useThemeMessage`, `ChatMessageContent`, `ChatPlatformBadges`. Здесь дублирование — это и
  есть дизайн.
- **`SignalRService`** — сложный, но осознанно: очередь на клиента решает конкретный баг
  с зависшим OBS.
- **`useSignalR.ts`** — та же история, цикл переподключения выстрадан.

---

## Зависимости между фазами

```
0 ──┬──> 1 ──┐
    ├──> 2 ──┼──> 3
    ├──> 4   │
    ├──> 5   │
    └──> 6   │
```

Фазы 1, 4, 5, 6 независимы — их можно делать в любом порядке и по отдельности.
Фаза 3 должна идти после 1 и 2, иначе придётся переносить код, который вот-вот исчезнет.
