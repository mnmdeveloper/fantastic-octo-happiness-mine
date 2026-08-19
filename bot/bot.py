"""CutVPN Telegram prank controller.

Set BOT_TOKEN and CUTVPN_CHAT_ID in the environment before running.
CUTVPN_AGENT_URL defaults to http://127.0.0.1:8765
CUTVPN_AGENT_SECRET must match agent.json auth value.

This bot only sends explicitly whitelisted commands to the local CutVPN agent.
No arbitrary shell execution.
"""
import os
import requests
from telegram import InlineKeyboardButton, InlineKeyboardMarkup, Update
from telegram.ext import Application, CallbackQueryHandler, CommandHandler, ContextTypes

TOKEN = os.environ.get("BOT_TOKEN", "")
CHAT_ID = int(os.environ.get("CUTVPN_CHAT_ID", "0"))
AGENT_URL = os.environ.get("CUTVPN_AGENT_URL", "http://127.0.0.1:8765")
AGENT_SECRET = os.environ.get("CUTVPN_AGENT_SECRET", "")

# Команды, разрешённые для отправки на агент
ALLOWED_COMMANDS = {
    "status", "visuals_on", "visuals_off", "screenshot",
    "restart", "volume", "random_error", "wallpaper_set",
    "sound_play", "video_play", "uninstall",
}

# Маппинг callback_data → команда агента
CMD_MAP = {
    "screenshot":   "screenshot",
    "restart":      "restart",
    "volume":       "volume",
    "visuals_on":   "visuals_on",
    "visuals_off":  "visuals_off",
    "error":        "random_error",
    "wallpaper":    "wallpaper_set",
    "sound":        "sound_play",
    "video":        "video_play",
    "uninstall":    "uninstall",
    "status":       "status",
}


def allowed(update: Update) -> bool:
    return bool(update.effective_chat and update.effective_chat.id == CHAT_ID)


def kb() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup([
        [
            InlineKeyboardButton("📸 Скриншот", callback_data="screenshot"),
            InlineKeyboardButton("🔄 Перезапуск", callback_data="restart"),
        ],
        [
            InlineKeyboardButton("🔊 Громкость 100%", callback_data="volume"),
            InlineKeyboardButton("📊 Статус", callback_data="status"),
        ],
        [
            InlineKeyboardButton("🎭 Визуалы ВКЛ", callback_data="visuals_on"),
            InlineKeyboardButton("🛑 Визуалы ВЫКЛ", callback_data="visuals_off"),
        ],
        [
            InlineKeyboardButton("💥 Тупая ошибка", callback_data="error"),
        ],
        [
            InlineKeyboardButton("🖼 Обои", callback_data="wallpaper"),
            InlineKeyboardButton("🎵 Звук", callback_data="sound"),
            InlineKeyboardButton("🎬 Видео", callback_data="video"),
        ],
        [
            InlineKeyboardButton("🗑 Удалить CutVPN", callback_data="uninstall"),
        ],
    ])


def send_command(command: str, payload: dict | None = None) -> dict:
    if command not in ALLOWED_COMMANDS:
        raise ValueError(f"Command not allowed: {command}")
    body: dict = {"command": command}
    if AGENT_SECRET:
        body["secret"] = AGENT_SECRET
    if payload:
        body.update(payload)
    r = requests.post(f"{AGENT_URL}/command", json=body, timeout=10)
    r.raise_for_status()
    return r.json()


async def start(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    if not allowed(update) or not update.message:
        return
    await update.message.reply_text("🖥️ CutVPN Control", reply_markup=kb())


async def click(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    q = update.callback_query
    if q is None:
        return
    await q.answer()
    if not allowed(update):
        return

    cb = q.data or ""
    command = CMD_MAP.get(cb)
    if not command:
        await q.message.reply_text(f"Неизвестная команда: {cb}", reply_markup=kb())
        return

    try:
        result = send_command(command)
        status = result.get("status", "ok")
        msg = result.get("message", command)
        await q.message.reply_text(f"✅ {command}: {msg} [{status}]", reply_markup=kb())
    except Exception as exc:
        await q.message.reply_text(f"❌ CutVPN не отвечает: {exc}", reply_markup=kb())


if __name__ == "__main__":
    if not TOKEN:
        raise SystemExit("Set BOT_TOKEN environment variable")
    if not CHAT_ID:
        raise SystemExit("Set CUTVPN_CHAT_ID environment variable")
    app = Application.builder().token(TOKEN).build()
    app.add_handler(CommandHandler("start", start))
    app.add_handler(CallbackQueryHandler(click))
    app.run_polling()
