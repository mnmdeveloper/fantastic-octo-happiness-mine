using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace CutVPN.Setup;

// ── Entry point ───────────────────────────────────────────────────────────────
internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.Run(new InstallerForm());
    }
}

// ── Config models ─────────────────────────────────────────────────────────────
internal sealed class InstallConfig
{
    public bool   Goose       { get; set; } = true;
    public bool   Cockroach   { get; set; } = true;
    public bool   Workrave    { get; set; } = true;
    public bool   Telemax     { get; set; } = true;
    public bool   Startup     { get; set; } = true;
    public string Name        { get; set; } = "";
    public string Nationality { get; set; } = "";
    public int    Children    { get; set; } = 0;
    public string Biography   { get; set; } = "";
}

internal sealed class CustomPage
{
    public string Title   { get; set; } = "";
    public string Body    { get; set; } = "";
    public string Color   { get; set; } = "#c0c0c0";
    public bool   IsError { get; set; } = false;
}

// ── Paths ─────────────────────────────────────────────────────────────────────
internal static class Paths
{
    public static string Root        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CutVPN");
    public static string Config      => Path.Combine(Root, "config.json");
    public static string AgentJson   => Path.Combine(Root, "agent.json");
    public static string AppExe      => Path.Combine(Root, "CutVPN.exe");
    public static string CustomPages => Path.Combine(Root, "custom-pages.json");
    public static string StartupCmd  => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "CutVPN.cmd");
}

// ══════════════════════════════════════════════════════════════════════════════
//  VIRTUAL KEYBOARD — алфавит в линию
// ══════════════════════════════════════════════════════════════════════════════
internal sealed class VirtualKeyboard : Form
{
    private readonly TextBox _target;
    private readonly string  _label;
    private bool _confirmed;

    // Цвета Win98
    static readonly Color C_BG    = Color.FromArgb(192, 192, 192);
    static readonly Color C_TITLE = Color.FromArgb(0, 0, 128);
    static readonly Color C_BTN   = Color.FromArgb(212, 208, 200);

    public VirtualKeyboard(TextBox target, string label)
    {
        _target = target;
        _label  = label;

        FormBorderStyle = FormBorderStyle.None;
        BackColor       = C_BG;
        Font            = new Font("MS Sans Serif", 8F);
        StartPosition   = FormStartPosition.CenterParent;
        ShowInTaskbar   = false;
        KeyPreview      = true;
        Width           = 860;
        Height          = 240;
        KeyDown        += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Build();
    }

    void Build()
    {
        Controls.Clear();

        // ── Title bar ──────────────────────────────────────────────────────
        var title = new Panel { Dock = DockStyle.Top, Height = 22, BackColor = C_TITLE };
        title.Controls.Add(new Label
        {
            Text      = $"Ввод: {_label}",
            ForeColor = Color.White,
            Font      = new Font("MS Sans Serif", 8F, FontStyle.Bold),
            AutoSize  = false,
            Dock      = DockStyle.Fill,
            Padding   = new Padding(4, 3, 0, 0),
        });
        var xBtn = new Button
        {
            Text      = "×",
            Size      = new Size(16, 14),
            Location  = new Point(Width - 18, 4),
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("MS Sans Serif", 7F),
            ForeColor = Color.White,
            BackColor = C_TITLE,
        };
        xBtn.FlatAppearance.BorderSize = 0;
        xBtn.Click += (_, _) => Close();
        title.Controls.Add(xBtn);
        Controls.Add(title);

        // ── Display field ──────────────────────────────────────────────────
        var display = new TextBox
        {
            Text      = _target.Text,
            ReadOnly  = true,
            Font      = new Font("Courier New", 12F),
            BackColor = Color.White,
            Dock      = DockStyle.Top,
            Height    = 32,
            Padding   = new Padding(4),
            BorderStyle = BorderStyle.Fixed3D,
        };
        Controls.Add(display);

        // ── Keyboard rows ──────────────────────────────────────────────────
        string[] rows =
        {
            "й ц у к е н г ш щ з х ъ",
            "ф ы в а п р о л д ж э",
            "я ч с м и т ь б ю . А Б В Г Д Е Ж З И К Л",
            "М Н О П Р С Т У Ф Х Ц Ч Ш Щ Ъ Ы Ь Э Ю Я",
            "a b c d e f g h i j k l m n o p q r s t u v w x y z",
            "A B C D E F G H I J K L M N O P Q R S T U V W X Y Z",
            "0 1 2 3 4 5 6 7 8 9 - _ . , ! ? @ # $ % & ( ) + = / \\"
        };

        var allKeys = new List<string>();
        foreach (var row in rows)
            foreach (var k in row.Split(' '))
                if (!string.IsNullOrEmpty(k) && !allKeys.Contains(k))
                    allKeys.Add(k);

        // Одна длинная строка кнопок, горизонтальный скролл
        var scroll = new Panel
        {
            Dock        = DockStyle.Fill,
            AutoScroll  = true,
            BackColor   = C_BG,
            Padding     = new Padding(6, 6, 6, 2),
        };
        Controls.Add(scroll);

        var keyPanel = new FlowLayoutPanel
        {
            AutoSize    = true,
            AutoSizeMode= AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor   = C_BG,
            Padding     = new Padding(0),
        };

        foreach (var ch in allKeys)
        {
            var k = ch; // capture
            var btn = new Button
            {
                Text      = k,
                Size      = new Size(k.Length > 1 ? 36 : 28, 26),
                Font      = new Font("Courier New", 9F),
                FlatStyle = FlatStyle.Standard,
                BackColor = C_BTN,
                Margin    = new Padding(2, 2, 2, 2),
                UseVisualStyleBackColor = false,
            };
            btn.Click += (_, _) =>
            {
                _target.Text += k;
                display.Text  = _target.Text;
            };
            keyPanel.Controls.Add(btn);
        }

        // Пробел
        var space = new Button
        {
            Text      = "ПРОБЕЛ",
            Size      = new Size(80, 26),
            Font      = new Font("MS Sans Serif", 8F),
            FlatStyle = FlatStyle.Standard,
            BackColor = C_BTN,
            Margin    = new Padding(2),
        };
        space.Click += (_, _) => { _target.Text += " "; display.Text = _target.Text; };
        keyPanel.Controls.Add(space);

        // Backspace
        var bs = new Button
        {
            Text      = "← Del",
            Size      = new Size(60, 26),
            Font      = new Font("MS Sans Serif", 8F),
            FlatStyle = FlatStyle.Standard,
            BackColor = C_BTN,
            Margin    = new Padding(2),
        };
        bs.Click += (_, _) =>
        {
            if (_target.Text.Length > 0) _target.Text = _target.Text[..^1];
            display.Text = _target.Text;
        };
        keyPanel.Controls.Add(bs);

        // OK
        var ok = new Button
        {
            Text      = "OK",
            Size      = new Size(50, 26),
            Font      = new Font("MS Sans Serif", 8F, FontStyle.Bold),
            BackColor = C_BTN,
            Margin    = new Padding(2),
        };
        ok.Click += (_, _) => { _confirmed = true; Close(); };
        keyPanel.Controls.Add(ok);

        scroll.Controls.Add(keyPanel);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
//  INSTALLER FORM — Win98 «Шиттинг ХУЯКА» style
// ══════════════════════════════════════════════════════════════════════════════
internal sealed class InstallerForm : Form
{
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h, int id, uint mod, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h, int id);

    const int  WM_HOTKEY = 0x0312;
    const int  HK_EXIT   = 7001;
    const int  HK_STOP   = 7002;
    const uint MOD_WIN   = 0x0008;
    const uint MOD_CTRL  = 0x0002;
    const uint MOD_SHIFT = 0x0004;
    const int  INSTALL_SECONDS = 150;

    // ── Win98 colours ─────────────────────────────────────────────────────────
    static readonly Color C_WIN_BG    = Color.FromArgb(58,  58,  58);   // тёмный фон окна (как в ref)
    static readonly Color C_TITLE_BAR = Color.FromArgb(0,   0,  128);
    static readonly Color C_SIDEBAR   = Color.FromArgb(30,  30,  30);
    static readonly Color C_MAIN_BG   = Color.FromArgb(192, 192, 192);
    static readonly Color C_BTN       = Color.FromArgb(212, 208, 200);
    static readonly Color C_INSET     = Color.FromArgb(128, 128, 128);

    // ── Page titles (sidebar) ─────────────────────────────────────────────────
    static readonly string[] SideSteps =
    {
        "Подготовка к\nшиттингу CutVPN",
        "Допрос\nсвидетелей",
        "Персональные\nпредложения",
        "Свойства:\nсясь кран",
        "Компоненты\nCutVPN",
        "Новости\nЧебурНета",
        "Шиттинг\nCutVPN",
        "Завершение\nдрочки",
    };

    // ── Sidebar icons (unicode symbols) ──────────────────────────────────────
    static readonly string[] SideIcons = { "■", "?", "■", "■", "■", "■", "■", "■" };

    // ── Controls ─────────────────────────────────────────────────────────────
    readonly Panel   pnlSidebar  = new();
    readonly Panel   pnlMain     = new();
    readonly Label   lblTitle    = new();   // big header line inside main
    readonly Panel   pnlContent  = new();
    readonly ProgressBar progress = new();
    readonly Label   lblProgress  = new();
    readonly Button  btnBack      = new() { Text = "< Назад" };
    readonly Button  btnNext      = new() { Text = "Далее >" };
    readonly Button  btnCancel    = new() { Text = "Отмена" };
    readonly System.Windows.Forms.Timer installTimer = new() { Interval = 1000 };

    // ── Page 2 – personal ────────────────────────────────────────────────────
    readonly TextBox txName        = new() { ReadOnly = true };
    readonly TextBox txNationality = new() { ReadOnly = true };
    readonly TextBox txBiography   = new() { ReadOnly = true };
    readonly NumericUpDown numChildren = new() { Minimum = 0, Maximum = 99, Width = 55, ReadOnly = true };
    bool nameOk, natOk, bioOk;

    // ── Page 4 – components ──────────────────────────────────────────────────
    readonly CheckBox chkGoose     = new() { Text = "Desktop Goose",        AutoSize = true, Checked = true };
    readonly CheckBox chkCockroach = new() { Text = "Cockroach on Desktop", AutoSize = true, Checked = true };
    readonly CheckBox chkWorkrave  = new() { Text = "Workrave",             AutoSize = true, Checked = true };
    readonly CheckBox chkTelemax   = new() { Text = "TELEMAX",              AutoSize = true, Checked = true };
    readonly CheckBox chkStartup   = new() { Text = "Запускать при входе в Windows", AutoSize = true, Checked = true };

    // ── Page 3 – crane ───────────────────────────────────────────────────────
    readonly ComboBox cmbGender = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    readonly TrackBar trkEmpire = new() { Minimum = 0, Maximum = 100, Value = 60, Width = 260 };
    readonly Label    lblEmpire = new() { AutoSize = true };

    List<CustomPage> customPages = new();
    int  page;
    int  elapsed;
    bool installing;

    static readonly string[] FakeStatus =
    {
        "Согласовываем вязанку с генсухой...",
        "Ищем OSEMENIT.Bimbim...",
        "Проверяем, где гусь...",
        "Опрашиваем тараканов о лицензии...",
        "Workrave пытается найти зрение...",
        "Загружаем Framework по доению коровы...",
        "Проверяем протокол ЧебурНета...",
        "Упорядочиваем вязанку...",
        "Гусь украл 0.7% прогресса...",
        "Уточняем у генсухи, можно ли продолжать...",
        "Ошибка: вязанка повернулась не той стороной",
        "OSEMENIT.Bimbim временно задумался",
        "Гусь запросил повышение зарплаты",
        "Тараканы не приняли лицензионное соглашение",
        "Загружаем ЧебурНет Core 3.14...",
        "Клопы требуют отдельный прогресс-бар...",
        "Вязанка обновилась на 0.003%...",
        "GENSUHA.dll одобряет операцию...",
    };

    // ─────────────────────────────────────────────────────────────────────────
    public InstallerForm()
    {
        if (File.Exists(Paths.CustomPages))
            try { customPages = JsonSerializer.Deserialize<List<CustomPage>>(File.ReadAllText(Paths.CustomPages)) ?? new(); } catch { }

        cmbGender.Items.AddRange(new object[] { "ANANAS SIPIR PRO(много)", "Гусь", "Вязанка", "Работает кран" });
        cmbGender.SelectedIndex = 0;
        trkEmpire.ValueChanged += (_, _) => lblEmpire.Text = $"{trkEmpire.Value * 640 / 100 + 320} на {trkEmpire.Value * 480 / 100 + 240} киллометров";
        lblEmpire.Text = "640 на 480 киллометров";

        // ── Window ────────────────────────────────────────────────────────────
        Text            = "Шиттинг CutVPN";
        FormBorderStyle = FormBorderStyle.None;
        WindowState     = FormWindowState.Maximized;
        BackColor       = C_WIN_BG;
        Font            = new Font("MS Sans Serif", 8F);
        KeyPreview      = true;

        // ── Title bar (emulated Win98) ────────────────────────────────────────
        var titleBar = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = C_TITLE_BAR };
        var titleIcon = new Label { Text = "♿  Шиттинг CutVPN", ForeColor = Color.White, Font = new Font("MS Sans Serif", 10F, FontStyle.Bold), AutoSize = true, Location = new Point(4, 5) };
        var xBtn = Win98Btn("×", 20, 18);
        xBtn.Dock = DockStyle.Right;
        xBtn.Click += (_, _) => Close();
        var minBtn = Win98Btn("_", 20, 18);
        minBtn.Dock = DockStyle.Right;
        minBtn.Click += (_, _) => WindowState = FormWindowState.Minimized;
        titleBar.Controls.Add(titleIcon);
        titleBar.Controls.Add(xBtn);
        titleBar.Controls.Add(minBtn);
        Controls.Add(titleBar);

        // ── Outer: sidebar + main ─────────────────────────────────────────────
        var outer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(6, 4, 6, 6) };
        outer.BackColor = C_WIN_BG;
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 172));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(outer);

        // ── Sidebar ───────────────────────────────────────────────────────────
        pnlSidebar.Dock      = DockStyle.Fill;
        pnlSidebar.BackColor = C_SIDEBAR;
        pnlSidebar.Padding   = new Padding(0, 8, 0, 0);
        Inset3D(pnlSidebar);
        outer.Controls.Add(pnlSidebar, 0, 0);

        // ── Main area ─────────────────────────────────────────────────────────
        pnlMain.Dock      = DockStyle.Fill;
        pnlMain.BackColor = C_MAIN_BG;
        pnlMain.Padding   = new Padding(0);
        Inset3D(pnlMain);
        outer.Controls.Add(pnlMain, 1, 0);

        // Main: big header strip
        var headerStrip = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = C_MAIN_BG };
        lblTitle.Font      = new Font("MS Sans Serif", 16F, FontStyle.Bold);
        lblTitle.ForeColor = Color.Black;
        lblTitle.AutoSize  = false;
        lblTitle.Dock      = DockStyle.Fill;
        lblTitle.Padding   = new Padding(10, 8, 0, 0);
        headerStrip.Controls.Add(lblTitle);
        pnlMain.Controls.Add(headerStrip);

        // horizontal divider
        var div = new Panel { Dock = DockStyle.Top, Height = 2, BackColor = C_INSET };
        pnlMain.Controls.Add(div);

        // Content area
        pnlContent.Dock      = DockStyle.Fill;
        pnlContent.BackColor = C_MAIN_BG;
        pnlContent.AutoScroll = true;
        pnlContent.Padding   = new Padding(14, 10, 14, 0);
        pnlMain.Controls.Add(pnlContent);

        // Progress bar (hidden until install page)
        var pbarWrap = new Panel { Dock = DockStyle.Bottom, Height = 44, BackColor = C_MAIN_BG, Padding = new Padding(10, 4, 10, 4), Visible = false };
        progress.Dock    = DockStyle.Top; progress.Height = 16; progress.Maximum = INSTALL_SECONDS;
        lblProgress.Dock = DockStyle.Fill; lblProgress.Font = new Font("MS Sans Serif", 7F);
        pbarWrap.Controls.Add(lblProgress); pbarWrap.Controls.Add(progress);
        pnlMain.Controls.Add(pbarWrap);
        // expose
        progress.Tag = pbarWrap;

        // ── Bottom button row ─────────────────────────────────────────────────
        var btnRow = new Panel { Dock = DockStyle.Bottom, Height = 38, BackColor = C_MAIN_BG, Padding = new Padding(8, 4, 8, 4) };
        btnRow.Controls.Add(Win98Sep(btnRow));
        var btnLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        foreach (var b in new[] { btnCancel, btnNext, btnBack })
        {
            b.Size      = new Size(80, 24);
            b.Font      = new Font("MS Sans Serif", 8F);
            b.FlatStyle = FlatStyle.Standard;
            b.BackColor = C_BTN;
            b.Margin    = new Padding(4, 0, 0, 0);
            btnLayout.Controls.Add(b);
        }
        btnRow.Controls.Add(btnLayout);
        pnlMain.Controls.Add(btnRow);

        // ── Bottom hint (below main area) ─────────────────────────────────────
        var hint = new Label
        {
            Text      = "До конца шиттинга:  Win+U / Esc — выйти   •   Ctrl+Shift+G — стоп",
            Dock      = DockStyle.Bottom,
            Height    = 20,
            ForeColor = Color.FromArgb(180, 180, 180),
            BackColor = C_WIN_BG,
            Font      = new Font("MS Sans Serif", 7F),
            Padding   = new Padding(4, 3, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        Controls.Add(hint);

        // ── Events ───────────────────────────────────────────────────────────
        btnBack.Click   += (_, _) => NavigatePage(-1);
        btnNext.Click   += (_, _) => TryNavigateNext();
        btnCancel.Click += (_, _) => { if (MessageBox.Show("Отменить установку?", "CutVPN", MessageBoxButtons.YesNo) == DialogResult.Yes) Close(); };
        installTimer.Tick += (_, _) => TickInstall();
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); if (e.KeyCode == Keys.G && e.Control && e.Shift) Close(); };
        Shown += (_, _) => { RegisterHotKey(Handle, HK_EXIT, MOD_WIN, (uint)Keys.U); RegisterHotKey(Handle, HK_STOP, MOD_CTRL | MOD_SHIFT, (uint)Keys.G); };
        FormClosed += (_, _) => { UnregisterHotKey(Handle, HK_EXIT); UnregisterHotKey(Handle, HK_STOP); installTimer.Stop(); };

        ShowPage();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  WndProc
    // ─────────────────────────────────────────────────────────────────────────
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY) { int id = m.WParam.ToInt32(); if (id == HK_EXIT || id == HK_STOP) { Close(); return; } }
        base.WndProc(ref m);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Navigation
    // ─────────────────────────────────────────────────────────────────────────
    int TotalPages => 8 + customPages.Count;

    void TryNavigateNext()
    {
        // Validate personal page
        if (page == 2)
        {
            if (!nameOk || string.IsNullOrWhiteSpace(txName.Text))
            { FlashRequired(txName, "Ваше имя"); return; }
            if (!natOk || string.IsNullOrWhiteSpace(txNationality.Text))
            { FlashRequired(txNationality, "Национальность"); return; }
            if (!bioOk || string.IsNullOrWhiteSpace(txBiography.Text))
            { FlashRequired(txBiography, "Биография"); return; }
        }
        NavigatePage(+1);
    }

    static void FlashRequired(Control c, string field)
    {
        MessageBox.Show($"Поле «{field}» обязательно для заполнения.\nНажмите на поле для ввода.", "CutVPN — обязательное поле", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        c.Focus();
    }

    void NavigatePage(int delta)
    {
        if (installing) return;
        page = Math.Clamp(page + delta, 0, TotalPages - 1);
        if (page == 6 && delta > 0) { elapsed = 0; installing = true; progress.Value = 0; installTimer.Start(); }
        ShowPage();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ShowPage
    // ─────────────────────────────────────────────────────────────────────────
    void ShowPage()
    {
        pnlContent.Controls.Clear();
        bool isFinal      = page == TotalPages - 1;
        bool isInstalling = page == 6;
        var  pbarWrap     = progress.Tag as Panel;
        if (pbarWrap != null) pbarWrap.Visible = isInstalling;

        btnBack.Enabled   = page > 0 && !isInstalling && !isFinal;
        btnNext.Enabled   = !isInstalling;
        btnCancel.Enabled = !isFinal;
        btnNext.Text      = isFinal ? "Готово" : "Далее >";
        if (isFinal)
        {
            btnNext.Click -= (_, _) => TryNavigateNext();
            btnNext.Click -= (_, _) => NavigatePage(+1);
            btnNext.Click += (_, _) => Close();
        }

        BuildSidebar();

        // Page title
        string[] builtinTitles = {
            "Подготовка к шиттингу CutVPN",
            "Параметры ЧебурНета для локальной сети",
            "Персональные предложения",
            "Свойства: сясь кран",
            "Компоненты CutVPN",
            "Новости ЧебурНета",
            "Шиттинг CutVPN",
            "Завершение дрочки",
        };
        lblTitle.Text = page < 8 ? builtinTitles[page] : customPages[page - 8].Title;

        var p = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0) };
        pnlContent.Controls.Add(p);

        switch (page)
        {
            case 0: PageWelcome(p);    break;
            case 1: PageInternet(p);   break;
            case 2: PagePersonal(p);   break;
            case 3: PageCrane(p);      break;
            case 4: PageComponents(p); break;
            case 5: PageNews(p);       break;
            case 6: PageInstalling(p); break;
            case 7: PageFinish(p);     break;
            default: PageCustom(p, customPages[page - 8]); break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Sidebar builder
    // ─────────────────────────────────────────────────────────────────────────
    void BuildSidebar()
    {
        pnlSidebar.Controls.Clear();

        // bottom label
        var bottom = new Label
        {
            Text      = "До конца шиттинга:\n" + (installing ? $"~{Math.Max(0, INSTALL_SECONDS - elapsed)} сек" : "много"),
            ForeColor = Color.Yellow,
            BackColor = C_SIDEBAR,
            Font      = new Font("MS Sans Serif", 7F, FontStyle.Bold),
            Dock      = DockStyle.Bottom,
            Height    = 36,
            Padding   = new Padding(10, 4, 0, 0),
        };
        pnlSidebar.Controls.Add(bottom);

        // soft logo
        var logo = new Label
        {
            Text      = "✦soft",
            ForeColor = Color.FromArgb(180, 160, 140),
            BackColor = C_SIDEBAR,
            Font      = new Font("MS Sans Serif", 9F, FontStyle.Italic),
            Dock      = DockStyle.Bottom,
            Height    = 24,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        pnlSidebar.Controls.Add(logo);

        int totalSteps = Math.Min(SideSteps.Length, 8);
        for (int i = totalSteps - 1; i >= 0; i--)
        {
            int idx = i;
            bool active  = (idx == Math.Min(page, 7));
            bool passed  = (idx < Math.Min(page, 7));

            var item = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 44,
                BackColor = active ? Color.FromArgb(50, 50, 100) : C_SIDEBAR,
                Cursor    = Cursors.Default,
                Padding   = new Padding(4, 4, 4, 4),
            };
            if (active) Raise3D(item);

            var icon = new Label
            {
                Text      = passed ? "■" : (active ? "?" : "■"),
                ForeColor = active ? Color.White : (passed ? Color.FromArgb(100,100,160) : Color.FromArgb(80, 80, 80)),
                BackColor = Color.Transparent,
                Font      = new Font("MS Sans Serif", 9F),
                AutoSize  = false,
                Size      = new Size(18, 38),
                Location  = new Point(4, 4),
                TextAlign = ContentAlignment.TopCenter,
            };
            var lbl = new Label
            {
                Text      = SideSteps[idx],
                ForeColor = active ? Color.White : (passed ? Color.FromArgb(120, 120, 180) : Color.FromArgb(140, 140, 140)),
                BackColor = Color.Transparent,
                Font      = active ? new Font("MS Sans Serif", 7F, FontStyle.Bold) : new Font("MS Sans Serif", 7F),
                AutoSize  = false,
                Size      = new Size(140, 38),
                Location  = new Point(24, 3),
            };
            item.Controls.Add(icon);
            item.Controls.Add(lbl);
            pnlSidebar.Controls.Add(item);

            // thin divider
            var sep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(60, 60, 60) };
            pnlSidebar.Controls.Add(sep);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PAGE 0 – Welcome
    // ─────────────────────────────────────────────────────────────────────────
    void PageWelcome(Panel p)
    {
        // Left: text block
        p.Controls.Add(Lbl("CutVPN обеспечивает поддержание жизни процессора для обеспечения\nработы с Филей и рабов капутеров с различными органическими течениями\nпо здоровью.", 0, 0, 520, 62));
        p.Controls.Add(Lbl("Доступно каждому жителю Земли!", 0, 70, 520, 24, 12, FontStyle.Bold));
        p.Controls.Add(Lbl(
            "Закамерный пиздец MAGNEFLAIR позволяет увеличивать численность\nнаселения до 7^92 раз! Так-же он имеет поддержку отрашивания\nвыбранной вами части тела, но я пока-что это не проверял.",
            0, 98, 520, 60));

        var g = Grp("Что будет установлено", 0, 168, 680, 160);
        g.Controls.Add(Lbl(
            "☑  Desktop Goose  — ПРОДАМ ГУСЯ\n" +
            "☑  Cockroach on Desktop  — уничтожение клопов\n" +
            "☑  Workrave  — улучшение зрения не вставая\n" +
            "☑  TELEMAX  — мессенджер\n" +
            "☑  Локальный агент  — порт 8765",
            10, 18, 640, 130));
        p.Controls.Add(g);
        p.Height = 350;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PAGE 1 – Internet
    // ─────────────────────────────────────────────────────────────────────────
    void PageInternet(Panel p)
    {
        p.Controls.Add(Lbl("Выберите способ настройки параметров прокси-сервера. Если вы не знаете,\nчто выбрать, выберите автоматическое определение настроек или обратитесь к\nсетевому администратору.", 0, 0, 680, 52));
        p.Controls.Add(Lbl("Использование автоматической настройки может изменить установленные\nвручную параметры. Для использования ручной настройки — отключите\nавтоматическую настройку.", 0, 56, 680, 52));

        var g = Grp("Автоматическая настройка", 0, 118, 680, 110);
        var chk1 = new CheckBox { Text = "Автоматическое определение прокси-сервера (рекомендуется)", Location = new Point(12, 22), AutoSize = true, Checked = true };
        var chk2 = new CheckBox { Text = "Использовать сценарий автоматической настройки", Location = new Point(12, 50), AutoSize = true };
        var lblAddr = new Label { Text = "Адрес:", Location = new Point(28, 82), AutoSize = true, ForeColor = Color.Gray };
        var txAddr  = new TextBox { Location = new Point(74, 78), Width = 380, Text = "", Enabled = false };
        g.Controls.Add(chk1); g.Controls.Add(chk2); g.Controls.Add(lblAddr); g.Controls.Add(txAddr);
        p.Controls.Add(g);

        p.Controls.Add(new CheckBox { Text = "Ручная настройка прокси-сервера", Location = new Point(2, 240), AutoSize = true });
        p.Height = 280;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PAGE 2 – Personal (обязательные поля, виртуальная клавиатура)
    // ─────────────────────────────────────────────────────────────────────────
    void PagePersonal(Panel p)
    {
        // Left image placeholder (как в referensе — диск Windows)
        var imgBox = new Panel
        {
            Location  = new Point(0, 0),
            Size      = new Size(120, 200),
            BackColor = Color.FromArgb(30, 30, 30),
            BorderStyle = BorderStyle.Fixed3D,
        };
        imgBox.Controls.Add(new Label
        {
            Text      = "ШИНДОВС\nCutVPN\n\n💿",
            ForeColor = Color.White,
            Font      = new Font("MS Sans Serif", 8F, FontStyle.Bold),
            AutoSize  = false,
            Size      = new Size(116, 196),
            TextAlign = ContentAlignment.MiddleCenter,
        });
        p.Controls.Add(imgBox);

        // Right side text + fields
        int lx = 134, rx = 270, fw = 340;

        p.Controls.Add(Lbl("Георгий просит вас предоставить все персональные\nданные для их обработки и хранения. Вся информация\nбудет храниться ДО 10380 дней. После этого времени вся\nсистема помрёт и георгий попросит вас заново повторить\nпроцедуру. Если вы согласны с этим то вы обязаны быть\nпосланны нахуй, согласно пользовательскому соглашению,\nкоторое вы приняли в начале шиттинга.", lx, 0, 560, 118));

        // ── Name ──────────────────────────────────────────────────────────
        p.Controls.Add(Lbl("Ваше имя:", lx, 128, 130, 20));
        txName.Location = new Point(lx + 130, 125); txName.Width = fw; txName.BackColor = nameOk ? Color.White : Color.FromArgb(255, 240, 240);
        txName.Click += (_, _) => OpenVK(txName, "Ваше имя", () => nameOk = !string.IsNullOrWhiteSpace(txName.Text));
        p.Controls.Add(txName);

        // ── Nationality ───────────────────────────────────────────────────
        p.Controls.Add(Lbl("Кто вы по\nнациональности?", lx, 158, 130, 36));
        txNationality.Location = new Point(lx + 130, 162); txNationality.Width = fw; txNationality.BackColor = natOk ? Color.White : Color.FromArgb(255, 240, 240);
        txNationality.Click += (_, _) => OpenVK(txNationality, "Национальность", () => natOk = !string.IsNullOrWhiteSpace(txNationality.Text));
        p.Controls.Add(txNationality);

        // ── Children ──────────────────────────────────────────────────────
        p.Controls.Add(Lbl("Количество\nдетей в семье:", lx, 206, 130, 36));
        numChildren.Location = new Point(lx + 130, 210);
        p.Controls.Add(numChildren);

        // ── Biography ────────────────────────────────────────────────────
        p.Controls.Add(Lbl("Ваша биография:", lx + 130, 248, 130, 20));
        txBiography.Location = new Point(lx + 130, 268); txBiography.Width = fw; txBiography.BackColor = bioOk ? Color.White : Color.FromArgb(255, 240, 240);
        txBiography.Click += (_, _) => OpenVK(txBiography, "Биография", () => bioOk = !string.IsNullOrWhiteSpace(txBiography.Text));
        p.Controls.Add(txBiography);

        // Placeholder hint
        p.Controls.Add(Lbl("* Все поля обязательны. Нажмите на поле для ввода.", lx, 308, 580, 20, 7, FontStyle.Italic));

        p.Height = 340;
    }

    void OpenVK(TextBox tx, string label, Action onClose)
    {
        var vk = new VirtualKeyboard(tx, label);
        vk.ShowDialog(this);
        tx.BackColor = string.IsNullOrWhiteSpace(tx.Text)
            ? Color.FromArgb(255, 240, 240)
            : Color.White;
        onClose();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PAGE 3 – Crane (Свойства: сясь кран)
    // ─────────────────────────────────────────────────────────────────────────
    void PageCrane(Panel p)
    {
        // Tabs (fake, не переключаются — как в оригинале)
        string[] tabs = { "Замени гудок!", "Запштафка", "Империя окон", "3D видеожопомонтаж", "ЧебурНет", "Настройка" };
        int tx = 0;
        foreach (var t in tabs)
        {
            bool active = t == "Настройка";
            var tb = new Button
            {
                Text      = t,
                Location  = new Point(tx, 0),
                Size      = new Size(Math.Max(80, t.Length * 7 + 8), 22),
                FlatStyle = FlatStyle.Standard,
                BackColor = active ? C_MAIN_BG : Color.FromArgb(180, 180, 180),
                Font      = new Font("MS Sans Serif", 7F, active ? FontStyle.Bold : FontStyle.Regular),
            };
            p.Controls.Add(tb);
            tx += tb.Width + 2;
        }

        // Monitor image (drawn)
        var monitor = new MonitorPanel { Location = new Point(120, 30), Size = new Size(240, 160) };
        p.Controls.Add(monitor);

        p.Controls.Add(Lbl("ваш sunsung кран:", 4, 198, 500, 16, 7, FontStyle.Bold));
        p.Controls.Add(Lbl("Нетрадиционный моне пися нахуй в VBE Miniport — Standard PCI\nGraphics Adapter (VGA)", 4, 214, 600, 32));

        // Gender group
        var gGender = Grp("Ваш гендер", 4, 254, 240, 80);
        cmbGender.Location = new Point(8, 20); gGender.Controls.Add(cmbGender);
        // Rainbow strip (drawn)
        var rainbow = new RainbowPanel { Location = new Point(8, 50), Size = new Size(220, 14) };
        gGender.Controls.Add(rainbow);
        p.Controls.Add(gGender);

        // Empire group
        var gEmpire = Grp("Область империи", 256, 254, 340, 80);
        gEmpire.Controls.Add(new Label { Text = "мало", Location = new Point(8, 28), AutoSize = true });
        gEmpire.Controls.Add(new Label { Text = "дохуя", Location = new Point(286, 28), AutoSize = true });
        trkEmpire.Location = new Point(44, 22); gEmpire.Controls.Add(trkEmpire);
        lblEmpire.Location = new Point(44, 52); gEmpire.Controls.Add(lblEmpire);
        p.Controls.Add(gEmpire);

        // Negro работают (checked, greyed)
        var chkNegro = new CheckBox { Text = "негры работают", Location = new Point(8, 348), AutoSize = true, Checked = true, Enabled = false };
        p.Controls.Add(chkNegro);

        var btnDop = Win98Btn("Дополнительно...", 120, 22);
        btnDop.Location = new Point(480, 344);
        btnDop.Click += (_, _) => MessageBox.Show("Дополнительных дополнений нет.\n640 на 480 киллометров.", "Дополнительно");
        p.Controls.Add(btnDop);

        p.Height = 390;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PAGE 4 – Components
    // ─────────────────────────────────────────────────────────────────────────
    void PageComponents(Panel p)
    {
        p.Controls.Add(Lbl("По умолчанию выбрано всё. Каждый компонент устанавливается явно из папки payload\\.", 0, 0, 680, 20));

        var gGoose = Grp("Desktop Goose", 0, 26, 680, 52);
        gGoose.Controls.Add(Lbl("«ПРОДАМ ГУСЯ» — один штука. Состояние: бегает.", 10, 14, 500, 18, 8, FontStyle.Bold));
        var btnBuy = Win98Btn("КУПИТЬ", 60, 20); btnBuy.Location = new Point(598, 22);
        btnBuy.Click += (_, _) => MessageBox.Show("Гусь продан.\nДоставка: не предусмотрена.", "CutVPN Market");
        gGoose.Controls.Add(btnBuy);
        chkGoose.Location = new Point(10, 32); gGoose.Controls.Add(chkGoose);
        p.Controls.Add(gGoose);

        var gCock = Grp("Cockroach on Desktop", 0, 86, 680, 52);
        gCock.Controls.Add(Lbl("Уничтожение клопов из дома (не гарантируется).", 10, 14, 500, 18, 8, FontStyle.Bold));
        chkCockroach.Location = new Point(10, 32); gCock.Controls.Add(chkCockroach);
        p.Controls.Add(gCock);

        var gWork = Grp("Workrave", 0, 146, 680, 52);
        gWork.Controls.Add(Lbl("Улучшение зрения, не вставая из-за ПК.", 10, 14, 500, 18, 8, FontStyle.Bold));
        chkWorkrave.Location = new Point(10, 32); gWork.Controls.Add(chkWorkrave);
        p.Controls.Add(gWork);

        var gTele = Grp("TELEMAX", 0, 206, 680, 52);
        gTele.Controls.Add(Lbl("Мессенджер для локальной сети.", 10, 14, 500, 18, 8, FontStyle.Bold));
        chkTelemax.Location = new Point(10, 32); gTele.Controls.Add(chkTelemax);
        p.Controls.Add(gTele);

        chkStartup.Location = new Point(4, 270); p.Controls.Add(chkStartup);
        p.Height = 310;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PAGE 5 – News
    // ─────────────────────────────────────────────────────────────────────────
    void PageNews(Panel p)
    {
        p.Controls.Add(Lbl("Проблемы при использовании", 0, 0, 680, 28, 14, FontStyle.Bold));
        p.Controls.Add(Lbl(
            "Навигация по ХУЯ-картам капутера и кредитным картам юзера стала ещё проще,\n" +
            "благодаря интеграции воров навигации: \"Геолокация\" и \"CVС код\". Они\n" +
            "перемещаются по папкам, Филе, парагурамамам и узлам ЧебурНета, сканируя\n" +
            "и находя кредитную историю текущего юзера.\n\n" +
            "Операции с капутером мы починили, поэтому пользоваться ХУЯКОМ я вам\n" +
            "запретил. Но! Вы можете выбрать нового бой-френда и стать курсором мыши,\n" +
            "просто наведя хуем хуй и щелкнув по хую хуем!\n\n" +
            "...Дочевож дошли-то тех-на-налогии!!",
            0, 34, 680, 220));
        p.Height = 270;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PAGE 6 – Installing
    // ─────────────────────────────────────────────────────────────────────────
    void PageInstalling(Panel p)
    {
        p.Controls.Add(Lbl("Шиттинг кончил хуярить хуём по куринному Филе! Теперь ваша ВМ\nбольше никогда не включится, не проверяйте даже. Да, и ещё, если\nты ещё будешь плохо себя вести, мы запретим тебе нажимать на\nлюбые кнопки.", 0, 0, 680, 78, 9, FontStyle.Bold));

        // Status
        var g1 = Grp("снесли файлов", 0, 88, 320, 200);
        g1.Controls.Add(Lbl(
            "  CutVPN.exe\n  config.json\n  agent.json\n" +
            (chkGoose.Checked     ? "► Desktop Goose\n" : "") +
            (chkCockroach.Checked ? "► Cockroach\n" : "") +
            (chkWorkrave.Checked  ? "► Workrave\n" : "") +
            (chkTelemax.Checked   ? "► TELEMAX\n" : ""),
            8, 18, 300, 175));
        p.Controls.Add(g1);

        var g2 = Grp("операции", 330, 88, 350, 200);
        g2.Controls.Add(Lbl(
            "☑ Проверка лицензии\n☑ Генсуха согласована\n☑ Вязанка загружена\n" +
            "☑ OSEMENIT.Bimbim ок\n☑ Гусь найден\n☑ Тараканы зарег.\n☑ ЧебурНет активирован",
            8, 18, 330, 175));
        p.Controls.Add(g2);

        lblProgress.Text = FakeStatus[0];
        p.Height = 310;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PAGE 7 – Finish
    // ─────────────────────────────────────────────────────────────────────────
    void PageFinish(Panel p)
    {
        p.Controls.Add(Lbl("Завершение дрочки", 0, 0, 680, 34, 14, FontStyle.Bold));
        p.Controls.Add(Lbl($"CutVPN установлен в:\n{Paths.Root}", 0, 40, 680, 36));

        var g = Grp("Итоги установки", 0, 84, 680, 180);
        g.Controls.Add(Lbl(
            $"Desktop Goose:      {(chkGoose.Checked     ? "✓ установлен" : "— пропущен")}\n" +
            $"Cockroach:          {(chkCockroach.Checked ? "✓ установлен" : "— пропущен")}\n" +
            $"Workrave:           {(chkWorkrave.Checked  ? "✓ установлен" : "— пропущен")}\n" +
            $"TELEMAX:            {(chkTelemax.Checked   ? "✓ установлен" : "— пропущен")}\n" +
            $"Автозапуск:         {(chkStartup.Checked   ? "✓ включён"    : "— выключен")}\n\n" +
            $"Агент: 127.0.0.1:8765\n" +
            $"Бот: настройте BOT_TOKEN в env",
            10, 20, 650, 150));
        p.Controls.Add(g);
        p.Height = 300;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Custom page
    // ─────────────────────────────────────────────────────────────────────────
    static void PageCustom(Panel p, CustomPage cp)
    {
        try { p.BackColor = ColorTranslator.FromHtml(cp.Color); } catch { }
        if (cp.IsError)
            p.Controls.Add(Lbl("⚠ ОШИБКА СИСТЕМЫ ЧЕБУРНЕТА", 0, 0, 680, 30, 13, FontStyle.Bold));
        p.Controls.Add(Lbl(cp.Body, 0, cp.IsError ? 38 : 0, 680, 400));
        p.Height = 440;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Launch component
    // ─────────────────────────────────────────────────────────────────────────
    void LaunchComponent(string key)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "payload");
        string[] cands = key switch
        {
            "goose"     => new[] { "DesktopGoose.Setup.exe", "DesktopGoose.exe", "DesktopGoose.msi" },
            "cockroach" => new[] { "CockroachOnDesktop.exe", "Cockroach.Setup.exe", "Cockroach.exe", "Cockroach.msi" },
            "workrave"  => new[] { "workrave-setup.exe", "Workrave.Setup.exe", "Workrave.exe", "Workrave.msi" },
            "telemax"   => new[] { "TELEMAX.exe", "telemax.exe", "Telemax.Setup.exe", "TELEMAX.msi" },
            _           => Array.Empty<string>()
        };
        foreach (var f in cands)
        {
            var full = Path.Combine(dir, f);
            if (!File.Exists(full)) continue;
            try { Process.Start(new ProcessStartInfo(full) { UseShellExecute = true, WorkingDirectory = dir }); return; }
            catch (Exception ex) { MessageBox.Show($"Не удалось запустить {f}:\n{ex.Message}", "CutVPN", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        // Не нашли — молча пропускаем
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Install tick
    // ─────────────────────────────────────────────────────────────────────────
    void TickInstall()
    {
        elapsed++;
        progress.Value   = Math.Min(elapsed, INSTALL_SECONDS);
        lblProgress.Text = $"{FakeStatus[(elapsed / 10) % FakeStatus.Length]}   ~{Math.Max(0, INSTALL_SECONDS - elapsed)} сек";

        if (elapsed == 15  && chkGoose.Checked)     LaunchComponent("goose");
        if (elapsed == 60  && chkCockroach.Checked) LaunchComponent("cockroach");
        if (elapsed == 100 && chkWorkrave.Checked)  LaunchComponent("workrave");
        if (elapsed == 130 && chkTelemax.Checked)   LaunchComponent("telemax");

        // rebuild sidebar to update counter
        BuildSidebar();

        if (elapsed >= INSTALL_SECONDS)
        {
            installTimer.Stop();
            installing = false;
            SaveConfig();
            page = 7;
            ShowPage();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Save config
    // ─────────────────────────────────────────────────────────────────────────
    void SaveConfig()
    {
        Directory.CreateDirectory(Paths.Root);
        var cfg = new InstallConfig
        {
            Goose       = chkGoose.Checked,
            Cockroach   = chkCockroach.Checked,
            Workrave    = chkWorkrave.Checked,
            Telemax     = chkTelemax.Checked,
            Startup     = chkStartup.Checked,
            Name        = txName.Text,
            Nationality = txNationality.Text,
            Children    = (int)numChildren.Value,
            Biography   = txBiography.Text,
        };
        File.WriteAllText(Paths.Config, JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Paths.AgentJson, JsonSerializer.Serialize(new
        {
            bind = "127.0.0.1", port = 8765, auth = "SET_LOCAL_SECRET",
            commands = new[] { "status","info","screenshot","visuals_on","visuals_off","volume_max","volume_mute","volume_set","random_error","wallpaper_set","sound_play","video_play","msgbox","restart","uninstall" }
        }, new JsonSerializerOptions { WriteIndented = true }));
        if (chkStartup.Checked)
            File.WriteAllText(Paths.StartupCmd, $"@echo off\r\nstart \"\" \"{Paths.AppExe}\" --installed\r\n");
        else if (File.Exists(Paths.StartupCmd))
            File.Delete(Paths.StartupCmd);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  UI Helpers
    // ─────────────────────────────────────────────────────────────────────────
    static Label Lbl(string text, int x, int y, int w, int h, float size = 8f, FontStyle fs = FontStyle.Regular)
        => new() { Text = text, Location = new Point(x, y), Size = new Size(w, h), Font = new Font("MS Sans Serif", size, fs), BackColor = Color.Transparent };

    static GroupBox Grp(string text, int x, int y, int w, int h)
        => new() { Text = text, Location = new Point(x, y), Size = new Size(w, h), Font = new Font("MS Sans Serif", 8F) };

    static Button Win98Btn(string text, int w, int h)
        => new() { Text = text, Size = new Size(w, h), FlatStyle = FlatStyle.Standard, BackColor = Color.FromArgb(212, 208, 200), Font = new Font("MS Sans Serif", 8F) };

    static void Inset3D(Control c)
    {
        c.Paint += (s, e) =>
        {
            var g = e.Graphics;
            var r = new Rectangle(0, 0, c.Width - 1, c.Height - 1);
            g.DrawLine(Pens.DimGray, r.Left, r.Top, r.Right, r.Top);
            g.DrawLine(Pens.DimGray, r.Left, r.Top, r.Left, r.Bottom);
            g.DrawLine(Pens.White,   r.Right, r.Top, r.Right, r.Bottom);
            g.DrawLine(Pens.White,   r.Left, r.Bottom, r.Right, r.Bottom);
        };
    }

    static void Raise3D(Control c)
    {
        c.Paint += (s, e) =>
        {
            var g = e.Graphics;
            var r = new Rectangle(0, 0, c.Width - 1, c.Height - 1);
            g.DrawLine(Pens.White,   r.Left, r.Top, r.Right, r.Top);
            g.DrawLine(Pens.White,   r.Left, r.Top, r.Left, r.Bottom);
            g.DrawLine(Pens.DimGray, r.Right, r.Top, r.Right, r.Bottom);
            g.DrawLine(Pens.DimGray, r.Left, r.Bottom, r.Right, r.Bottom);
        };
    }

    static Panel Win98Sep(Control parent)
        => new() { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(128, 128, 128) };
}

// ── Custom drawn panels ───────────────────────────────────────────────────────
internal sealed class MonitorPanel : Panel
{
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        // Monitor body
        g.FillRectangle(new SolidBrush(Color.FromArgb(192, 192, 192)), 20, 0, 200, 130);
        g.DrawRectangle(Pens.Black, 20, 0, 200, 130);
        // Screen
        g.FillRectangle(new SolidBrush(Color.FromArgb(0, 128, 128)), 30, 8, 180, 100);
        // Win95 desktop sim
        g.FillRectangle(new SolidBrush(Color.FromArgb(0, 128, 128)), 30, 8, 180, 100);
        g.FillRectangle(new SolidBrush(Color.FromArgb(0, 0, 128)), 30, 98, 180, 10);
        // Icons
        g.FillRectangle(Brushes.White, 35, 15, 18, 18);
        g.FillRectangle(Brushes.White, 35, 38, 18, 18);
        g.FillRectangle(Brushes.White, 35, 61, 18, 18);
        g.FillRectangle(Brushes.White, 60, 15, 18, 18);
        g.FillRectangle(Brushes.White, 60, 38, 18, 18);
        // Stand
        g.FillRectangle(new SolidBrush(Color.FromArgb(160, 160, 160)), 95, 130, 50, 18);
        g.FillRectangle(new SolidBrush(Color.FromArgb(140, 140, 140)), 75, 148, 90, 6);
    }
}

internal sealed class RainbowPanel : Panel
{
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var brush = new LinearGradientBrush(ClientRectangle,
            Color.Red, Color.Violet, LinearGradientMode.Horizontal);
        brush.InterpolationColors = new ColorBlend
        {
            Colors = new[] { Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Blue, Color.Indigo, Color.Violet },
            Positions = new[] { 0f, 0.17f, 0.33f, 0.5f, 0.67f, 0.83f, 1f }
        };
        e.Graphics.FillRectangle(brush, ClientRectangle);
    }
}
