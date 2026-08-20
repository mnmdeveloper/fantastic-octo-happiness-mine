using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace CutVPN;

// ─────────────────────────────────────────────────────────────────────────────
//  Entry
// ─────────────────────────────────────────────────────────────────────────────
internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.Run(new CutVpnTray());
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Paths
// ─────────────────────────────────────────────────────────────────────────────
internal static class Paths
{
    public static string Root     => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CutVPN");
    public static string Config   => Path.Combine(Root, "config.json");
    public static string State    => Path.Combine(Root, "prank.state");
    public static string AgentJson => Path.Combine(Root, "agent.json");
}

// ─────────────────────────────────────────────────────────────────────────────
//  Tray application
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class CutVpnTray : ApplicationContext
{
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h, int id, uint mod, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h, int id);

    readonly NotifyIcon  tray;
    readonly HotkeyWindow hkWnd;
    bool visuals;

    static readonly string[] RandomErrors =
    {
        "Ошибка 0xGUSB: буфер переполнен вязанкой",
        "GENSUHA.dll недоступна: генсуха пьёт чай",
        "OSEMENIT.Bimbim не отвечает (задумался)",
        "Гусь заблокировал системный вызов",
        "Вязанка не прошла проверку целостности",
        "Тараканы не приняли EULA",
        "Framework по доению коровы упал с ошибкой 418",
        "CutVPN: подключение разорвано гусём",
        "Ядро Чебурнета: требуется перезагрузка мировоззрения",
    };

    internal CutVpnTray()
    {
        visuals = LoadVisuals();

        tray = new NotifyIcon
        {
            Icon    = BuildIcon(),
            Text    = "CutVPN — Cheburetnet",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };
        tray.DoubleClick += (_, _) => ShowDashboard();

        // Hotkey window
        hkWnd = new HotkeyWindow();
        hkWnd.OnStopKey += () =>
        {
            visuals = false; SaveVisuals(false);
            tray.ShowBalloonTip(1200, "CutVPN", "Стоп-кран сработал. Визуалы отключены.", ToolTipIcon.Warning);
        };

        // Start local agent
        AgentServer.Start();
        AgentServer.OnCommand += HandleAgentCommand;
    }

    // ── tray icon (painted in code) ───────────────────────────────────────────
    static Icon BuildIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(0, 0, 128));
        using var f = new Font("Tahoma", 16f, FontStyle.Bold);
        g.DrawString("C", f, Brushes.White, 2, 4);
        return Icon.FromHandle(bmp.GetHicon());
    }

    ContextMenuStrip BuildMenu()
    {
        var m = new ContextMenuStrip { Font = new Font("Tahoma", 9f) };

        var header = new ToolStripMenuItem("CutVPN  v1.0") { Enabled = false };
        m.Items.Add(header);
        m.Items.Add(new ToolStripSeparator());

        var statusItem = new ToolStripMenuItem("Статус: ONLINE") { Enabled = false, ForeColor = Color.DarkGreen };
        m.Items.Add(statusItem);
        m.Items.Add(new ToolStripSeparator());

        m.Items.Add("Открыть панель управления", null, (_, _) => ShowDashboard());
        m.Items.Add(new ToolStripSeparator());

        m.Items.Add("Включить визуалы",        null, (_, _) => { visuals = true;  SaveVisuals(true);  Notify("Визуалы включены."); });
        m.Items.Add("Отключить визуалы",       null, (_, _) => { visuals = false; SaveVisuals(false); Notify("Визуалы отключены."); });
        m.Items.Add("Случайная тупая ошибка",  null, (_, _) => ShowRandomError());
        m.Items.Add("Перезапустить CutVPN",    null, (_, _) => Restart());
        m.Items.Add(new ToolStripSeparator());

        m.Items.Add("Удалить CutVPN",          null, (_, _) => Uninstall());
        m.Items.Add("Выход",                   null, (_, _) => Exit());
        return m;
    }

    // ── dashboard ─────────────────────────────────────────────────────────────
    void ShowDashboard()
    {
        var f = new DashboardForm(this);
        f.Show();
        f.BringToFront();
    }

    // ── helpers ───────────────────────────────────────────────────────────────
    internal void ShowRandomError()
    {
        var msg = RandomErrors[Random.Shared.Next(RandomErrors.Length)];
        MessageBox.Show(msg, "CutVPN — Диагностика Чебурнета", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    internal string GetStatus() => "ONLINE";
    internal bool   Visuals     => visuals;

    internal void SetVisuals(bool v) { visuals = v; SaveVisuals(v); }

    void Notify(string msg) => tray.ShowBalloonTip(900, "CutVPN", msg, ToolTipIcon.Info);

    void Restart()
    {
        var exe = Application.ExecutablePath;
        Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
        Exit();
    }

    void Uninstall()
    {
        if (MessageBox.Show("Удалить CutVPN?\nЭто удалит все файлы из %LOCALAPPDATA%\\CutVPN\\.",
                "CutVPN Uninstall", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            var cmd = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "CutVPN.cmd");
            if (File.Exists(cmd)) File.Delete(cmd);
            if (Directory.Exists(Paths.Root)) Directory.Delete(Paths.Root, true);
        }
        catch { }
        Exit();
    }

    void Exit()
    {
        AgentServer.Stop();
        hkWnd.Destroy();
        tray.Visible = false;
        tray.Dispose();
        Application.ExitThread();
    }

    void HandleAgentCommand(string command, JsonElement body, Action<object> reply)
    {
        switch (command)
        {
            case "status":      reply(new { status = "ok", message = GetStatus() }); break;
            case "visuals_on":  SetVisuals(true);  reply(new { status = "ok", message = "visuals on" }); break;
            case "visuals_off": SetVisuals(false); reply(new { status = "ok", message = "visuals off" }); break;
            case "random_error":
                Application.OpenForms[0]?.BeginInvoke(() => ShowRandomError());
                reply(new { status = "ok", message = "error shown" });
                break;
            case "restart":
                Application.OpenForms[0]?.BeginInvoke(() => Restart());
                reply(new { status = "ok", message = "restarting" });
                break;
            case "screenshot":
                var path = TakeScreenshot();
                reply(new { status = "ok", message = path });
                break;
            case "uninstall":
                Application.OpenForms[0]?.BeginInvoke(() => Uninstall());
                reply(new { status = "ok", message = "uninstalling" });
                break;
            default:
                reply(new { status = "error", message = $"unknown command: {command}" });
                break;
        }
    }

    static string TakeScreenshot()
    {
        try
        {
            var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            using var bmp = new Bitmap(bounds.Width, bounds.Height);
            using var g   = Graphics.FromImage(bmp);
            g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            var out_ = Path.Combine(Paths.Root, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            bmp.Save(out_, System.Drawing.Imaging.ImageFormat.Png);
            return out_;
        }
        catch (Exception e) { return $"error: {e.Message}"; }
    }

    static bool LoadVisuals()
    {
        if (File.Exists(Paths.State)) return File.ReadAllText(Paths.State).Trim() == "on";
        return true;
    }
    static void SaveVisuals(bool v)
    {
        Directory.CreateDirectory(Paths.Root);
        File.WriteAllText(Paths.State, v ? "on" : "off");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Dashboard form
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class DashboardForm : Form
{
    readonly CutVpnTray app;
    readonly Label statusLbl;

    internal DashboardForm(CutVpnTray app)
    {
        this.app = app;
        Text             = "CutVPN — Панель управления";
        StartPosition    = FormStartPosition.CenterScreen;
        ClientSize       = new Size(820, 520);
        FormBorderStyle  = FormBorderStyle.FixedSingle;
        MaximizeBox      = false;
        BackColor        = Color.FromArgb(236, 233, 216);
        Font             = new Font("Tahoma", 9F);

        // header
        var hdr = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(0, 0, 128) };
        hdr.Controls.Add(new Label { Text = "CutVPN  Control Panel", ForeColor = Color.White, Font = new Font("Tahoma", 15F, FontStyle.Bold), AutoSize = true, Location = new Point(16, 10) });
        Controls.Add(hdr);

        // body
        var body = new Panel { Location = new Point(14, 62), Size = new Size(792, 400), BorderStyle = BorderStyle.Fixed3D, BackColor = Color.White };
        Controls.Add(body);

        // logo area
        var logo = new Panel { Location = new Point(18, 18), Size = new Size(80, 80), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(0, 0, 128) };
        var logoTxt = new Label { Text = "C", ForeColor = Color.White, Font = new Font("Tahoma", 36F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };
        logo.Controls.Add(logoTxt);
        body.Controls.Add(logo);

        body.Controls.Add(new Label { Text = "CutVPN  1.0  Cheburetnet Edition", Font = new Font("Tahoma", 14F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 0, 128), Location = new Point(116, 22), AutoSize = true });
        body.Controls.Add(new Label { Text = "Очень серьёзный сетевой продукт с абсолютно несерьёзным содержимым.", Location = new Point(118, 56), AutoSize = true, ForeColor = Color.DimGray });

        statusLbl = new Label { Text = "● Статус: ONLINE", Location = new Point(116, 80), AutoSize = true, Font = new Font("Tahoma", 11F, FontStyle.Bold), ForeColor = Color.DarkGreen };
        body.Controls.Add(statusLbl);

        // server selector
        body.Controls.Add(new Label { Text = "VPN-сервер:", Location = new Point(26, 120), AutoSize = true });
        var srv = new ComboBox { Location = new Point(120, 116), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
        srv.Items.AddRange(new object[] { "CutVPN Europe (Чебурнет)", "Gensuha Turbo", "OSEMENIT.Bimbim", "Automatic" });
        srv.SelectedIndex = 0;
        body.Controls.Add(srv);

        var btnConnect = new Button { Text = "Подключить", Location = new Point(438, 114), Size = new Size(130, 28) };
        btnConnect.Click += (_, _) =>
        {
            bool on = statusLbl.Text.Contains("ONLINE");
            statusLbl.Text      = on ? "● Статус: OFFLINE" : "● Статус: ONLINE";
            statusLbl.ForeColor = on ? Color.Maroon : Color.DarkGreen;
            btnConnect.Text     = on ? "Подключить" : "Отключить";
        };
        body.Controls.Add(btnConnect);

        // buttons
        void Btn(string text, int x, int y, EventHandler h)
        {
            var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(180, 30) };
            b.Click += h;
            body.Controls.Add(b);
        }

        Btn("Включить визуалы",       26,  170, (_, _) => { app.SetVisuals(true);  MessageBox.Show("Визуалы включены."); });
        Btn("Отключить визуалы",      224, 170, (_, _) => { app.SetVisuals(false); MessageBox.Show("Визуалы отключены."); });
        Btn("Случайная ошибка",       422, 170, (_, _) => app.ShowRandomError());
        Btn("Открыть конструктор",    620, 170, (_, _) => OpenConstructor());

        body.Controls.Add(new Label { Text = "Инфо:", Location = new Point(26, 220), AutoSize = true });
        body.Controls.Add(new Label
        {
            Text      = $"Агент: http://127.0.0.1:8765\nConfig: {Paths.Config}\nТелеграм-бот: BOT_TOKEN + CUTVPN_CHAT_ID",
            Location  = new Point(80, 216),
            Size      = new Size(680, 70),
            Font      = new Font("Tahoma", 9F)
        });

        body.Controls.Add(new Label { Text = "Визуалы:", Location = new Point(26, 304), AutoSize = true });
        var vis = new CheckBox { Text = "Включить prank visuals", Location = new Point(100, 300), AutoSize = true, Checked = app.Visuals };
        vis.CheckedChanged += (_, _) => app.SetVisuals(vis.Checked);
        body.Controls.Add(vis);

        // footer
        Controls.Add(new Label
        {
            Text      = "CutVPN  •  Ctrl+Shift+G — стоп-кран  •  Агент слушает на 127.0.0.1:8765",
            Location  = new Point(14, 472),
            AutoSize  = true,
            ForeColor = Color.DimGray,
            Font      = new Font("Tahoma", 8F)
        });
    }

    static void OpenConstructor()
    {
        var pagesDir = Path.Combine(
            Path.GetDirectoryName(Application.ExecutablePath) ?? ".",
            "pages");
        var html = Path.Combine(pagesDir, "index.html");
        if (File.Exists(html))
            Process.Start(new ProcessStartInfo(html) { UseShellExecute = true });
        else
            MessageBox.Show($"Конструктор не найден:\n{html}", "CutVPN", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Hotkey message-only window
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class HotkeyWindow : NativeWindow
{
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h, int id, uint mod, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h, int id);

    const uint MOD_CTRL = 0x0002, MOD_SHIFT = 0x0004;
    const int  WM_HOTKEY = 0x0312;

    public event Action? OnStopKey;

    internal HotkeyWindow()
    {
        CreateHandle(new CreateParams());
        RegisterHotKey(Handle, 9001, MOD_CTRL | MOD_SHIFT, (uint)Keys.G);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == 9001) OnStopKey?.Invoke();
        base.WndProc(ref m);
    }

    internal void Destroy()
    {
        UnregisterHotKey(Handle, 9001);
        DestroyHandle();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Local agent HTTP server (loopback only)
// ─────────────────────────────────────────────────────────────────────────────
internal static class AgentServer
{
    static System.Net.HttpListener? listener;
    static Thread? thread;
    static string secret = "";

    public static event Action<string, JsonElement, Action<object>>? OnCommand;

    internal static void Start()
    {
        // Read secret from agent.json if present
        if (File.Exists(Paths.AgentJson))
        {
            try
            {
                var j = JsonDocument.Parse(File.ReadAllText(Paths.AgentJson));
                if (j.RootElement.TryGetProperty("auth", out var s)) secret = s.GetString() ?? "";
            }
            catch { }
        }

        listener = new System.Net.HttpListener();
        listener.Prefixes.Add("http://127.0.0.1:8765/");
        try { listener.Start(); }
        catch { return; } // port already taken — skip

        thread = new Thread(Loop) { IsBackground = true, Name = "AgentServer" };
        thread.Start();
    }

    internal static void Stop() => listener?.Stop();

    static readonly HashSet<string> Allowed = new()
    {
        "status","visuals_on","visuals_off","screenshot",
        "restart","volume","random_error","wallpaper_set",
        "sound_play","video_play","uninstall"
    };

    static void Loop()
    {
        while (listener?.IsListening == true)
        {
            System.Net.HttpListenerContext ctx;
            try { ctx = listener.GetContext(); }
            catch { break; }

            Task.Run(() => Handle(ctx));
        }
    }

    static void Handle(System.Net.HttpListenerContext ctx)
    {
        ctx.Response.ContentType = "application/json";

        if (ctx.Request.HttpMethod != "POST" || ctx.Request.Url?.AbsolutePath != "/command")
        {
            Respond(ctx, 404, new { status = "error", message = "not found" });
            return;
        }

        JsonDocument doc;
        try
        {
            using var sr = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
            doc = JsonDocument.Parse(sr.ReadToEnd());
        }
        catch { Respond(ctx, 400, new { status = "error", message = "invalid json" }); return; }

        var body = doc.RootElement;

        // Auth
        if (!string.IsNullOrEmpty(secret))
        {
            var provided = body.TryGetProperty("secret", out var s) ? s.GetString() : null;
            if (provided != secret) { Respond(ctx, 403, new { status = "error", message = "forbidden" }); return; }
        }

        var command = body.TryGetProperty("command", out var c) ? c.GetString() ?? "" : "";
        if (!Allowed.Contains(command)) { Respond(ctx, 400, new { status = "error", message = $"unknown: {command}" }); return; }

        object? reply = null;
        OnCommand?.Invoke(command, body, r => reply = r);
        Respond(ctx, 200, reply ?? new { status = "ok" });
    }

    static void Respond(System.Net.HttpListenerContext ctx, int code, object data)
    {
        ctx.Response.StatusCode = code;
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data));
        ctx.Response.OutputStream.Write(bytes);
        ctx.Response.Close();
    }
}
