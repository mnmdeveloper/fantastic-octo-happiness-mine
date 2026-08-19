"""CutVPN Local Agent — слушает только на 127.0.0.1:8765.

Принимает POST /command с JSON {"command": "...", "secret": "..."}
Выполняет только явно разрешённые команды. Без произвольного shell.

Запуск:
    python agent.py

Конфиг читается из %LOCALAPPDATA%\CutVPN\agent.json
"""
import json
import os
import subprocess
import sys
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path

# --- Config ---
LOCALAPPDATA = os.environ.get("LOCALAPPDATA", str(Path.home() / "AppData" / "Local"))
CONFIG_PATH = Path(LOCALAPPDATA) / "CutVPN" / "agent.json"

DEFAULT_BIND = "127.0.0.1"
DEFAULT_PORT = 8765
DEFAULT_SECRET = ""  # пустой = auth отключён

ALLOWED_COMMANDS = {
    "status", "visuals_on", "visuals_off", "screenshot",
    "restart", "volume", "random_error", "wallpaper_set",
    "sound_play", "video_play", "uninstall",
}


def load_config() -> dict:
    if CONFIG_PATH.exists():
        try:
            return json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
        except Exception:
            pass
    return {"bind": DEFAULT_BIND, "port": DEFAULT_PORT, "auth": DEFAULT_SECRET}


cfg = load_config()
BIND = cfg.get("bind", DEFAULT_BIND)
PORT = int(cfg.get("port", DEFAULT_PORT))
SECRET = cfg.get("auth", DEFAULT_SECRET)

# --- Command handlers ---

def cmd_status() -> str:
    return "CutVPN agent online"


def cmd_visuals_on() -> str:
    # TODO: IPC к CutVPN.exe
    return "visuals enabled (stub)"


def cmd_visuals_off() -> str:
    return "visuals disabled (stub)"


def cmd_screenshot() -> str:
    import datetime
    out = Path(LOCALAPPDATA) / "CutVPN" / f"screenshot_{datetime.datetime.now().strftime('%Y%m%d_%H%M%S')}.png"
    try:
        # Используем PowerShell для скриншота (Windows only, без внешних зависимостей)
        ps = (
            f"Add-Type -AssemblyName System.Windows.Forms; "
            f"$b=[System.Drawing.Bitmap]::new([System.Windows.Forms.SystemInformation]::VirtualScreen.Width,"
            f"[System.Windows.Forms.SystemInformation]::VirtualScreen.Height); "
            f"$g=[System.Drawing.Graphics]::FromImage($b); "
            f"$g.CopyFromScreen([System.Windows.Forms.SystemInformation]::VirtualScreen.Location,"
            f"[System.Drawing.Point]::Empty,$b.Size); "
            f"$b.Save('{out}'); $g.Dispose(); $b.Dispose()"
        )
        subprocess.run(["powershell", "-NonInteractive", "-Command", ps], timeout=15, check=True)
        return str(out)
    except Exception as e:
        return f"screenshot error: {e}"


def cmd_restart() -> str:
    cutvpn_exe = Path(LOCALAPPDATA) / "CutVPN" / "CutVPN.exe"
    if cutvpn_exe.exists():
        subprocess.Popen([str(cutvpn_exe), "--installed"])
        return "restarting CutVPN"
    return "CutVPN.exe not found"


def cmd_volume() -> str:
    try:
        ps = "(New-Object -ComObject WScript.Shell).SendKeys([char]174+[char]174+[char]174+[char]175+[char]175+[char]175+[char]175+[char]175+[char]175+[char]175+[char]175+[char]175+[char]175)"
        # Проще: устанавливаем 100% через nircmd или PowerShell audio API
        # Используем безопасный PowerShell без произвольного input
        ps2 = "$obj = New-Object -ComObject WScript.Shell; $obj.SendKeys([char]175*50)"
        subprocess.run(["powershell", "-NonInteractive", "-Command", ps2], timeout=5)
        return "volume increased"
    except Exception as e:
        return f"volume error: {e}"


def cmd_random_error() -> str:
    import random
    errors = [
        "GENSUHA.dll не найдена",
        "Вязанка не отвечает на запросы",
        "Гусь заблокировал доступ к системе",
        "OSEMENIT.Bimbim временно задумался",
        "Тараканы не приняли лицензионное соглашение",
        "Framework по доению коровы недоступен",
        "Ошибка 0xGUSB: буфер переполнен вязанкой",
    ]
    msg = random.choice(errors)
    try:
        ps = f'[System.Windows.Forms.MessageBox]::Show("{msg}", "CutVPN Error", "OK", "Error")'
        subprocess.Popen(["powershell", "-NonInteractive", "-Command",
                         f"Add-Type -AssemblyName System.Windows.Forms; {ps}"])
    except Exception:
        pass
    return msg


def cmd_wallpaper_set(path: str | None = None) -> str:
    if not path:
        return "no wallpaper path provided"
    wp = Path(path)
    if not wp.exists():
        return f"file not found: {path}"
    try:
        import ctypes
        SPI_SETDESKWALLPAPER = 20
        ctypes.windll.user32.SystemParametersInfoW(SPI_SETDESKWALLPAPER, 0, str(wp), 3)
        return f"wallpaper set: {path}"
    except Exception as e:
        return f"wallpaper error: {e}"


def cmd_sound_play(path: str | None = None) -> str:
    if not path:
        return "no sound path provided"
    if not Path(path).exists():
        return f"file not found: {path}"
    try:
        subprocess.Popen(["powershell", "-NonInteractive", "-Command",
                         f"(New-Object Media.SoundPlayer '{path}').PlaySync()"])
        return f"playing: {path}"
    except Exception as e:
        return f"sound error: {e}"


def cmd_video_play(path: str | None = None) -> str:
    if not path:
        return "no video path provided"
    if not Path(path).exists():
        return f"file not found: {path}"
    try:
        os.startfile(path)  # открывает дефолтным плеером
        return f"playing video: {path}"
    except Exception as e:
        return f"video error: {e}"


def cmd_uninstall() -> str:
    cutvpn_dir = Path(LOCALAPPDATA) / "CutVPN"
    startup_cmd = Path(os.environ.get("APPDATA", "")) / "Microsoft" / "Windows" / "Start Menu" / "Programs" / "Startup" / "CutVPN.cmd"
    removed = []
    if startup_cmd.exists():
        startup_cmd.unlink()
        removed.append("startup entry")
    try:
        import shutil
        if cutvpn_dir.exists():
            shutil.rmtree(cutvpn_dir)
            removed.append("CutVPN directory")
    except Exception as e:
        return f"uninstall partial: {', '.join(removed)} — error: {e}"
    return f"uninstalled: {', '.join(removed) or 'nothing to remove'}"


HANDLERS: dict[str, any] = {
    "status":       lambda body: cmd_status(),
    "visuals_on":   lambda body: cmd_visuals_on(),
    "visuals_off":  lambda body: cmd_visuals_off(),
    "screenshot":   lambda body: cmd_screenshot(),
    "restart":      lambda body: cmd_restart(),
    "volume":       lambda body: cmd_volume(),
    "random_error": lambda body: cmd_random_error(),
    "wallpaper_set":lambda body: cmd_wallpaper_set(body.get("path")),
    "sound_play":   lambda body: cmd_sound_play(body.get("path")),
    "video_play":   lambda body: cmd_video_play(body.get("path")),
    "uninstall":    lambda body: cmd_uninstall(),
}


class AgentHandler(BaseHTTPRequestHandler):
    def log_message(self, fmt, *args):  # заглушить стандартный лог
        pass

    def do_POST(self):
        if self.path != "/command":
            self._respond(404, {"status": "error", "message": "not found"})
            return

        length = int(self.headers.get("Content-Length", 0))
        try:
            body: dict = json.loads(self.rfile.read(length))
        except Exception:
            self._respond(400, {"status": "error", "message": "invalid json"})
            return

        # Auth check
        if SECRET and body.get("secret") != SECRET:
            self._respond(403, {"status": "error", "message": "forbidden"})
            return

        command = body.get("command", "")
        if command not in ALLOWED_COMMANDS:
            self._respond(400, {"status": "error", "message": f"unknown command: {command}"})
            return

        try:
            result = HANDLERS[command](body)
            self._respond(200, {"status": "ok", "message": result})
        except Exception as exc:
            self._respond(500, {"status": "error", "message": str(exc)})

    def _respond(self, code: int, data: dict):
        body = json.dumps(data).encode()
        self.send_response(code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)


if __name__ == "__main__":
    server = HTTPServer((BIND, PORT), AgentHandler)
    print(f"CutVPN Agent listening on {BIND}:{PORT}", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("Agent stopped.")
        sys.exit(0)
