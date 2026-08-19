# CutVPN Telegram bot

## Setup

```powershell
py -m pip install -r requirements.txt
$env:BOT_TOKEN = "YOUR_BOT_TOKEN"
$env:CUTVPN_CHAT_ID = "YOUR_TELEGRAM_CHAT_ID"
$env:CUTVPN_AGENT_URL = "http://127.0.0.1:8765"
py bot.py
```

The bot allowlists a single chat ID. Keep the bot token private and do not commit it.

Commands exposed by the starter controller:
- screenshot
- restart
- volume
- visuals on/off
- random prank error
- uninstall CutVPN

The Windows agent should validate the same command allowlist and expose only local, authenticated control.
