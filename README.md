# Fantastic Octo Happiness Mine

A Windows desktop companion controlled through Telegram.

## Scope

This project is designed for use on a computer you own or administer. It provides explicit remote-management features such as screenshots, restart, volume control, wallpaper/media playback, and desktop effects.

It does **not** implement stealthy malware behavior, hidden installation, credential theft, persistence designed to evade detection, or a fake/cracked software installer intended to trick another user.

## Planned architecture

```text
Telegram Bot (controller PC)
        |
        | Telegram Bot API
        v
Windows Agent (controlled PC)
   |- screenshot
   |- restart
   |- volume
   |- wallpaper library
   |- sound/video playback
   |- desktop effects
   `- status/health
```

## Repository layout

- `agent/` - Windows agent
- `bot/` - Telegram bot/controller
- `installer/` - transparent installer/build scripts
- `media/` - optional local media assets
- `.github/workflows/` - CI/build automation

## Security

The agent should authenticate Telegram requests by an allow-list of Telegram user IDs and keep its bot token outside source control.
