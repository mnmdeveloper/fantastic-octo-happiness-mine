# CutVPN — Мастер шиттинга Чебурнета

Полный стек пранк-установщика с фейковым Windows-мастером, локальным агентом управления и Telegram-ботом.

---

## Структура проекта

```
cutvpn/
├── installer/          # Установщик (C#, WinForms, .NET 8)
│   ├── Program.Good.cs     # Исходник мастера установки
│   ├── CutVPN.Setup.csproj # Проект
│   ├── build.ps1           # Скрипт сборки
│   └── payload/            # Сюда кладите реальные установщики компонентов
│       ├── DesktopGoose.Setup.exe   (скачайте сами)
│       ├── CockroachOnDesktop.exe   (скачайте сами)
│       └── workrave-setup.exe       (скачайте сами)
│
├── agent/              # Локальный HTTP-агент (Python, stdlib only)
│   ├── agent.py            # Агент на 127.0.0.1:8765
│   └── requirements.txt    # Нет внешних зависимостей (stdlib)
│
├── bot/                # Telegram-бот для управления
│   ├── bot.py              # Бот с inline-клавиатурой
│   └── requirements.txt    # python-telegram-bot, requests
│
└── pages/              # Веб-конструктор пользовательских страниц
    └── index.html          # Открыть в браузере
```

---

## Быстрый старт

### 1. Сборка установщика

```powershell
cd installer
powershell -ExecutionPolicy Bypass -File build.ps1
```

Результат: `installer\bin\Release\net8.0-windows\win-x64\publish\CutVPN.Setup.exe`

> **Требования:** .NET 8 SDK

### 2. Добавьте компоненты в payload\

Скачайте официальные установщики и положите в `installer\payload\`:

| Компонент | Файл |
|-----------|------|
| Desktop Goose | `DesktopGoose.Setup.exe` / `DesktopGoose.exe` |
| Cockroach on Desktop | `CockroachOnDesktop.exe` / `Cockroach.exe` |
| Workrave | `workrave-setup.exe` / `Workrave.exe` |

Если файл не найден — компонент пропускается (установщик продолжает работу).

### 3. Запустите агент

```bash
cd agent
python agent.py
```

Агент слушает на `127.0.0.1:8765`. Конфиг создаётся установщиком в `%LOCALAPPDATA%\CutVPN\agent.json`.

### 4. Настройте и запустите Telegram-бот

```bash
cd bot
pip install -r requirements.txt

set BOT_TOKEN=ваш_токен_от_BotFather
set CUTVPN_CHAT_ID=ваш_chat_id
set CUTVPN_AGENT_SECRET=SET_LOCAL_SECRET

python bot.py
```

Узнать свой chat_id: напишите `/start` боту [@userinfobot](https://t.me/userinfobot).

### 5. Веб-конструктор страниц

Откройте `pages/index.html` в браузере. Создайте страницы, скачайте `custom-pages.json` и положите в:
```
%LOCALAPPDATA%\CutVPN\custom-pages.json
```
Установщик автоматически добавит ваши страницы после встроенных.

---

## Команды агента (POST /command)

```json
{ "command": "...", "secret": "SET_LOCAL_SECRET" }
```

| Команда | Описание |
|---------|----------|
| `status` | Статус агента (CPU, RAM если есть psutil) |
| `info` | Информация о системе |
| `screenshot` | Скриншот → возвращает base64 |
| `visuals_on` / `visuals_off` | Включить / выключить визуальные эффекты |
| `volume_max` | Громкость 100% |
| `volume_mute` | Тихий режим |
| `volume_set` | `{"level": 50}` — установить уровень |
| `random_error` | Случайное окно ошибки Чебурнета |
| `wallpaper_set` | `{"path": "C:\\...\\img.jpg"}` |
| `sound_play` | `{"path": "C:\\...\\file.wav"}` |
| `video_play` | `{"path": "C:\\...\\file.mp4"}` |
| `msgbox` | `{"title": "Заголовок", "text": "Текст", "icon": "Warning"}` |
| `restart` | Перезапустить CutVPN.exe |
| `uninstall` | Удалить CutVPN с машины |

---

## Горячие клавиши установщика

| Клавиша | Действие |
|---------|----------|
| `Esc` | Закрыть установщик |
| `Win + U` | Закрыть (глобально) |
| `Ctrl + Shift + G` | Аварийный стоп |

---

## Сброс / выход из пранка

Жертва может удалить CutVPN вручную:
- Удалить `%LOCALAPPDATA%\CutVPN\`
- Удалить `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\CutVPN.cmd`

Или через бот: кнопка **🗑 УДАЛИТЬ CutVPN**.
