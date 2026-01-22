# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Run Commands

```bash
# Build
dotnet build
dotnet build -c Release

# Run the application
dotnet run --project UkiChat/UkiChat.csproj

# Run tests
dotnet test
dotnet test --filter "ClassName"  # Run specific test class
```

## Configuration

Create `UkiChat/app-settings.local.toml` based on `app-settings.template.toml`:
```toml
[database]
password = "your_database_password"
[twitch.chat]
username = "your_twitch_username"
access_token = "your_twitch_access_token"  # from twitchtokengenerator.com
```

## Architecture Overview

**Hybrid WPF + Web Application**: WPF desktop app with an embedded WebView2 browser that loads a Vue.js frontend served via Kestrel at `http://localhost:5000`.

### Layer Structure

```
WPF UI (MainWindow, SettingsWindow, ProfileWindow)
    ↕ WebView2
Vue.js Frontend (wwwroot/)
    ↕ SignalR WebSocket
AppHub (Hubs/AppHub.cs) - Central API for frontend
    ↓
Services Layer (Services/) - Business logic
    ↓
Repository Layer (Repositories/) - LiteDB data access
    ↓
LiteDB Database
```

### Key Components

- **App.xaml.cs**: Application bootstrap - initializes Kestrel HTTP server and DI container
- **Configuration/DIConfiguration.cs**: Service registration via Scrutor assembly scanning (all services in `UkiChat.Services` namespace auto-registered as singletons)
- **Configuration/HttpServerConfiguration.cs**: Kestrel + SignalR setup
- **Hubs/AppHub.cs**: SignalR hub - all frontend-backend communication goes through here

### DI Container Setup

Dual container: **DryIoc** (Prism) + **Microsoft.Extensions.DependencyInjection** integrated via `DryIoc.Microsoft.DependencyInjection`.

Services are auto-scanned from `UkiChat.Services` namespace and registered as singletons. To add a new service:
1. Create interface `IMyService` and implementation `MyService` in `Services/`
2. It will be automatically registered

### MVVM Pattern (Prism)

- ViewModels in `ViewModels/` namespace follow naming convention: `{ViewName}ViewModel`
- View-to-ViewModel binding configured in `App.xaml.cs` via `ViewModelLocationProvider`
- Prism EventAggregator used for cross-component communication (events defined in `Events/`)

### Main Services

- **IStreamService**: Twitch chat connection via TwitchLib
- **IDatabaseService**: Database queries and settings management
- **ISignalRService**: Sends events to connected web clients
- **ILocalizationService**: Language switching (ru/en), JSON files in `Localization/`
- **IWindowService**: Window management for WPF

### Data Layer

- **Entities/**: Domain models with LiteDB attributes
- **Repositories/**: Data access classes for LiteDB
- **Model/**: DTOs for SignalR communication (Chat/, Settings/, Info/)

## Language

Code comments are in Russian.