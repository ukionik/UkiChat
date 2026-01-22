# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

UkiChat Web is a real-time chat aggregation frontend for streaming platforms (Twitch, YouTube, Kick, VK Video Live). It's a Nuxt 4 web UI that communicates with a WPF desktop backend via SignalR. The frontend focuses on displaying chat messages and settings, while all platform connections and business logic are handled by the backend.

## Development Commands

```bash
pnpm install        # Install dependencies
pnpm dev            # Start dev server at http://localhost:3000
pnpm build          # Build for production (outputs to ../UkiChat/UkiChat/wwwroot)
pnpm generate       # Generate static site
pnpm preview        # Preview production build
```

**Note:** No test or lint scripts are currently configured.

## Tech Stack

- **Nuxt 4** with Vue 3 Composition API
- **TypeScript** for type safety
- **@nuxt/ui** for UI components (UButton, UIcon, UForm, etc.)
- **Tailwind CSS** for styling
- **@microsoft/signalr** for real-time backend communication
- **Valibot** for schema validation
- **@nuxtjs/i18n** for internationalization (default locale: Russian)

## Architecture

### Backend Communication

All data flows through SignalR hub at `http://localhost:5000/apphub`. The frontend does not make HTTP calls directly.

Key composables:
- `app/composables/useSignalR.ts` - SignalR connection management with `invokeGet()` and `invokeUpdate()` methods
- `app/composables/useLocalization.ts` - Fetches translations from backend

### SignalR Hub Methods

**Queries (via `invokeGet`):**
- `GetActiveAppSettingsInfo` - Returns profile name, language, Twitch channel
- `GetActiveAppSettingsData` - Returns full settings object
- `GetLanguage(language)` - Returns JSON translation strings

**Commands (via `invokeUpdate`):**
- `OpenSettingsWindow` - Opens settings dialog
- `ConnectToTwitch` - Initiates Twitch connection
- `UpdateTwitchSettings(settings)` - Updates Twitch channel

**Events (listened on frontend):**
- `OnChatMessage` - New chat message received
- `OnTwitchReconnect` - Reconnection event

### Main Pages

- `pages/index.vue` - Main chat display with auto-scrolling, message history (max 1000 messages), platform icons
- `pages/settings.vue` - Twitch channel configuration with Valibot validation
- `pages/debug.vue` - Debug/testing page

### Types

- `app/types/ChatMessage.ts` - Chat message interface (platform, displayName, message)

## Build Output

Production builds output to `../UkiChat/UkiChat/wwwroot` for integration with the WPF backend application.

## UI Conventions

- Dark mode enabled by default (set in `app/app.vue`)
- Uses Nuxt UI components with Tailwind CSS utility classes
- Platform icons stored in `public/images/` (twitch.svg, youtube.svg, kick.svg, vk-video-live.svg)
