@echo off
:: Запуск CutVPN Telegram Bot
:: Заполните переменные ниже или используйте .env

set BOT_TOKEN=ВСТАВЬ_ТОКЕН
set CUTVPN_CHAT_ID=ВСТАВЬ_CHAT_ID
set CUTVPN_AGENT_SECRET=SET_LOCAL_SECRET
set CUTVPN_AGENT_URL=http://127.0.0.1:8765

echo [CutVPN Bot] Запуск...
pip install -q -r requirements.txt
python bot.py
pause
