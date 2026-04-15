# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Structure

```
UkiChat/          # .NET 9 WPF backend (solution root)
  UkiChat/        # Main project
  UkiChat.Tests/  # xUnit tests
ukichat-web/      # Nuxt 4 frontend
```

Each subdirectory has its own CLAUDE.md with detailed guidance. This file covers the cross-project architecture.

## Commands

**Backend** (run from `UkiChat/`):
```bash
dotnet build
dotnet run --project UkiChat/UkiChat.csproj
dotnet test
dotnet test --filter "ClassName"   # single test class
```

**Frontend** (run from `ukichat-web/`):
```bash
pnpm install
pnpm dev          # dev server at http://localhost:3000
pnpm build        # outputs to ../UkiChat/UkiChat/wwwroot
```

## Cross-Project Architecture

The app is a **WPF desktop host** that embeds a **Nuxt/Vue frontend** via WebView2. There is no HTTP client on the frontend — all communication is through a single SignalR hub.

```
Streaming Platforms (Twitch, VK Video Live)
    ↓
TwitchChatService / VkVideoLiveChatService   (parse messages, resolve badges/emotes)
    ↓
SignalRService   (broadcasts to all connected web clients)
    ↓  WebSocket
AppHub  (Hubs/AppHub.cs)   ←→   useSignalR.ts composable
    ↓
WebView2  →  Vue pages (index.vue = chat, settings.vue = config)
```

### SignalR Hub Methods (`Hubs/AppHub.cs`)

| Method | Direction | Purpose |
|---|---|---|
| `GetActiveAppSettingsInfo` | Frontend → Backend | Profile name, language, channels |
| `GetActiveAppSettingsData` | Frontend → Backend | Full settings object |
| `GetLanguage(lang)` | Frontend → Backend | Load translation JSON |
| `ChangeTwitchChannel(channel)` | Frontend → Backend | Switch Twitch channel |
| `ChangeVkVideoLiveChannel(channel)` | Frontend → Backend | Switch VK channel |
| `UpdateTwitchSettings(data)` | Frontend → Backend | Save + reconnect Twitch |
| `UpdateVkVideoLiveSettings(data)` | Frontend → Backend | Save + reconnect VK |
| `OpenSettingsWindow` | Frontend → Backend | Open WPF settings window |
| `OnChatMessage` | Backend → Frontend | New chat message |
| `OnTwitchReconnect` | Backend → Frontend | Twitch reconnect signal |
| `OnVkVideoLiveReconnect` | Backend → Frontend | VK reconnect signal |

### Build Integration

`pnpm build` in `ukichat-web/` writes static files to `UkiChat/UkiChat/wwwroot/`, which is then served by Kestrel and loaded by WebView2 when the WPF app starts.

## Code Language

Code comments are written in Russian.
