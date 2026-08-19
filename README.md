# CutVPN

CutVPN is a personal Windows desktop utility controlled through Telegram.

## Scope

- Windows agent with explicit installation and visible status
- Telegram bot running on a separate machine
- Screenshot capture
- Safe system actions such as restart
- Volume control
- Wallpaper/media library
- Optional desktop effects and break reminders

The installer identifies itself as **CutVPN** and does not impersonate unrelated or cracked software.

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

The agent should authenticate Telegram requests by an allow-list of Telegram user IDs and keep its bot token outside source control. Installation and runtime status remain visible to the local user.
