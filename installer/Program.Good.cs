using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace CutVPN.Setup;

// ─────────────────────────────────────────────────────────────────────────────
//  Entry point
// ─────────────────────────────────────────────────────────────────────────────
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

// ─────────────────────────────────────────────────────────────────────────────
//  Config model
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class InstallConfig
{
    public bool Goose { get; set; } = true;
    public bool Cockroach { get; set; } = true;
    public bool Workrave { get; set; } = true;
    public bool Startup { get; set; } = true;
    public bool Visuals { get; set; } = true;
    public bool TelemaxJoke { get; set; } = true;
    public string Nationality { get; set; } = "Чебурек";
    public int Children { get; set; } = 1;
    public string FavoriteGoose { get; set; } = "Серый обычный";
    public string Empire { get; set; } = "Империя Чебурнета";
}

// ─────────────────────────────────────────────────────────────────────────────
//  Paths
// ─────────────────────────────────────────────────────────────────────────────
internal static class Paths
{
    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CutVPN");
    public static string Config    => Path.Combine(Root, "config.json");
    public static string AgentJson => Path.Combine(Root, "agent.json");
    public static string AppExe    => Path.Combine(Root, "CutVPN.exe");
    public static string Joke      => Path.Combine(Root, "telemax-joke.txt");
    public static string Installed => Path.Combine(Root, "installed-components.txt");
    public static string CustomPages => Path.Combine(Root, "custom-pages.json");
    public static string StartupCmd  => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Startup), "CutVPN.cmd");
}

// ─────────────────────────────────────────────────────────────────────────────
//  Custom page model (from web constructor)
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class CustomPage
{
    public string Title   { get; set; } = "";
    public string Body    { get; set; } = "";
    public string Color   { get; set; } = "#f2f2f2";
    public bool   IsError { get; set; } = false;
}

// ─────────────────────────────────────────────────────────────────────────────
//  Main installer form
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class InstallerForm : Form
{
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h, int id, uint mod, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h, int id);

    const int    WM_HOTKEY = 0x0312;
    const int    HK_EXIT   = 7001;
    const int    HK_STOP   = 7002;
    const uint   MOD_WIN   = 0x0008;
    const uint   MOD_CTRL  = 0x0002;
    const uint   MOD_SHIFT = 0x0004;
    const int    INSTALL_SECONDS = 150;

    // ── controls ──────────────────────────────────────────────────────────────
    readonly Label   titleLbl    = new();
    readonly Label   subtitleLbl = new();
    readonly Panel   content     = new();
    readonly ProgressBar progress = new();
    readonly Label   progressTxt = new();
    readonly Button  btnBack     = new() { Text = "< Назад" };
    readonly Button  btnNext     = new() { Text = "Далее >" };
    readonly Button  btnCancel   = new() { Text = "Отмена" };
    readonly System.Windows.Forms.Timer installTimer = new() { Interval = 1000 };
    readonly Label   leftInfo    = new();

    // ── page 4 – components ───────────────────────────────────────────────────
    readonly CheckBox chkGoose     = new() { Text = "Desktop Goose", AutoSize = true, Checked = true };
    readonly CheckBox chkCockroach = new() { Text = "Cockroach on Desktop", AutoSize = true, Checked = true };
    readonly CheckBox chkWorkrave  = new() { Text = "Workrave", AutoSize = true, Checked = true };
    readonly CheckBox chkStartup   = new() { Text = "Запускать CutVPN при входе в Windows", AutoSize = true, Checked = true };
    readonly CheckBox chkTelemax   = new() { Text = "TELEMAX — ЭКСТРЕМИСТСКИЙ КЛИЕНТ (шутка)", AutoSize = true, Checked = true };

    // ── page 2 – personal ────────────────────────────────────────────────────
    readonly TextBox  txNationality = new() { Text = "Чебурек", Width = 260 };
    readonly NumericUpDown numChildren = new() { Minimum = 0, Maximum = 99, Value = 1, Width = 70 };
    readonly ComboBox cmbGoose = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };

    // ── page 3 – crane ────────────────────────────────────────────────────────
    readonly ComboBox cmbGender = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 310 };
    readonly ComboBox cmbEmpire = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 310 };
    readonly TrackBar trackCrane = new() { Minimum = 0, Maximum = 100, Value = 70, Width = 360 };

    int  page;
    int  elapsed;
    bool installing;
    List<CustomPage> customPages = new();

    static readonly string[] FakeStatuses =
    {
        "Согласовываем вязанку с генсухой...",
        "Ищем OSEMENIT.Bimbim...",
        "Проверяем, где гусь...",
        "Опрашиваем тараканов о лицензии...",
        "Workrave пытается найти зрение...",
        "Загружаем Framework по доению коровы...",
        "Проверяем протокол Чебурнета...",
        "Упорядочиваем вязанку...",
        "Гусь украл 0.7% прогресса...",
        "Уточняем у генсухи, можно ли продолжать...",
        "Ошибка: вязанка повернулась не той стороной",
        "OSEMENIT.Bimbim временно задумался",
        "Гусь запросил повышение зарплаты",
        "Тараканы не приняли лицензионное соглашение",
        "Загружаем Cheburetnet Core...",
        "Синхронизируем с генсухой (3 из 47 попыток)...",
        "Клопы требуют отдельный прогресс-бар...",
        "Вязанка обновилась на 0.003%...",
    };

    static readonly string[] BreakingNews =
    {
        "СРОЧНО! ВЯЗАНКА СНОВА БЫЛА ЗАМЕЧЕНА РЯДОМ С ГЕНСУХОЙ.",
        "ГУСЬ ОТКАЗАЛСЯ ДАВАТЬ КОММЕНТАРИИ.",
        "Framework по доению коровы получил очередное обновление.",
        "OSEMENIT.Bimbim успешно найден.",
        "GENSUHA.dll одобрила вязанку.",
        "Клопы объявили себя юридическим лицом.",
        "Тараканы подали заявление в Роскомнадзор.",
        "Источник: редакция «Чебурнет сегодня», состоящая из одного гуся.",
    };

    // ─────────────────────────────────────────────────────────────────────────
    //  Constructor
    // ─────────────────────────────────────────────────────────────────────────
    public InstallerForm()
    {
        // Load custom pages from web constructor if present
        if (File.Exists(Paths.CustomPages))
        {
            try { customPages = JsonSerializer.Deserialize<List<CustomPage>>(File.ReadAllText(Paths.CustomPages)) ?? new(); }
            catch { }
        }

        // Populate combos
        cmbGoose.Items.AddRange(new object[] { "Серый обычный", "Белый с претензиями", "Гусь в шляпе", "OSEMENIT.Bimbim" });
        cmbGoose.SelectedIndex = 0;
        cmbGender.Items.AddRange(new object[] { "АНАНАС SPIR PRO(много)", "Гусь", "Вязанка", "Работает кран" });
        cmbGender.SelectedIndex = 0;
        cmbEmpire.Items.AddRange(new object[] { "Империя Чебурнета", "Гусландия", "Вязаночная область", "Территория генсухи" });
        cmbEmpire.SelectedIndex = 0;

        // Window
        Text              = "Мастер шиттинга Чебурнета — CutVPN Setup";
        FormBorderStyle   = FormBorderStyle.None;
        WindowState       = FormWindowState.Maximized;
        BackColor         = Color.FromArgb(192, 192, 192);
        Font              = new Font("Tahoma", 9F);
        KeyPreview        = true;

        // ── top bar ──────────────────────────────────────────────────────────
        var top = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.FromArgb(0, 0, 128) };
        var topTitle = new Label
        {
            Text      = "Мастер установки CutVPN  —  Cheburetnet Edition",
            ForeColor = Color.White,
            Font      = new Font("Tahoma", 14F, FontStyle.Bold),
            AutoSize  = true,
            Location  = new Point(18, 16)
        };
        var topX = new Button
        {
            Text     = "✕",
            Size     = new Size(42, 34),
            Location = new Point(0, 0),
            Dock     = DockStyle.Right,
            Font     = new Font("Tahoma", 11F, FontStyle.Bold),
            FlatStyle= FlatStyle.Flat
        };
        topX.FlatAppearance.BorderSize = 0;
        topX.Click += (_, _) => Close();
        top.Controls.Add(topTitle);
        top.Controls.Add(topX);
        Controls.Add(top);

        // ── outer layout ─────────────────────────────────────────────────────
        var layout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 2,
            Padding     = new Padding(14, 10, 14, 0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 252));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        Controls.Add(layout);

        // ── left sidebar ─────────────────────────────────────────────────────
        var left = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(215, 215, 215), BorderStyle = BorderStyle.Fixed3D };
        leftInfo.Location  = new Point(14, 14);
        leftInfo.Size      = new Size(216, 520);
        leftInfo.Font      = new Font("Tahoma", 9F);
        left.Controls.Add(leftInfo);
        layout.Controls.Add(left, 0, 0);

        // ── right main area ───────────────────────────────────────────────────
        var main = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.Fixed3D, Padding = new Padding(18) };
        layout.Controls.Add(main, 1, 0);

        var head = new Panel { Dock = DockStyle.Top, Height = 94 };
        titleLbl.Dock      = DockStyle.Top; titleLbl.Height = 40;
        titleLbl.Font      = new Font("Tahoma", 17F, FontStyle.Bold);
        titleLbl.ForeColor = Color.FromArgb(0, 0, 128);
        subtitleLbl.Dock   = DockStyle.Fill;
        subtitleLbl.Font   = new Font("Tahoma", 10F);
        head.Controls.Add(subtitleLbl);
        head.Controls.Add(titleLbl);
        main.Controls.Add(head);

        content.Dock        = DockStyle.Fill;
        content.BackColor   = Color.FromArgb(240, 240, 240);
        content.BorderStyle = BorderStyle.Fixed3D;
        content.AutoScroll  = true;
        main.Controls.Add(content);

        var pbar = new Panel { Dock = DockStyle.Bottom, Height = 58, BackColor = Color.White };
        progress.Dock    = DockStyle.Top; progress.Height  = 20; progress.Maximum = INSTALL_SECONDS; progress.Visible = false;
        progressTxt.Dock = DockStyle.Fill; progressTxt.Font = new Font("Tahoma", 9F); progressTxt.Visible = false;
        pbar.Controls.Add(progressTxt);
        pbar.Controls.Add(progress);
        main.Controls.Add(pbar);

        // ── bottom button row ─────────────────────────────────────────────────
        var buttons = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 4,
            BackColor   = Color.FromArgb(212, 212, 212),
            Padding     = new Padding(5, 12, 5, 8)
        };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
        var hint = new Label
        {
            Text    = "Win+U / Esc — выйти   •   Ctrl+Shift+G — аварийный стоп",
            Dock    = DockStyle.Fill,
            ForeColor = Color.DimGray,
            Font    = new Font("Tahoma", 8F),
            Padding = new Padding(6, 5, 0, 0)
        };
        buttons.Controls.Add(hint, 0, 0);
        foreach (var b in new[] { btnBack, btnNext, btnCancel })
        {
            b.Dock   = DockStyle.Fill;
            b.Margin = new Padding(4, 0, 4, 8);
            b.Font   = new Font("Tahoma", 9F);
        }
        buttons.Controls.Add(btnBack,   1, 0);
        buttons.Controls.Add(btnNext,   2, 0);
        buttons.Controls.Add(btnCancel, 3, 0);
        layout.Controls.Add(buttons, 0, 1);
        layout.SetColumnSpan(buttons, 2);

        // ── event wiring ──────────────────────────────────────────────────────
        btnBack.Click   += (_, _) => NavigatePage(-1);
        btnNext.Click   += (_, _) => NavigatePage(+1);
        btnCancel.Click += (_, _) =>
        {
            if (MessageBox.Show("Отменить установку CutVPN?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Close();
        };
        installTimer.Tick += (_, _) => TickInstall();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) Close();
            if (e.KeyCode == Keys.G && e.Control && e.Shift) Close();
        };
        Shown += (_, _) =>
        {
            RegisterHotKey(Handle, HK_EXIT, MOD_WIN,  (uint)Keys.U);
            RegisterHotKey(Handle, HK_STOP, MOD_CTRL | MOD_SHIFT, (uint)Keys.G);
        };
        FormClosed += (_, _) =>
        {
            UnregisterHotKey(Handle, HK_EXIT);
            UnregisterHotKey(Handle, HK_STOP);
            installTimer.Stop();
        };

        ShowPage();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  WndProc (global hotkeys)
    // ─────────────────────────────────────────────────────────────────────────
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY)
        {
            var id = m.WParam.ToInt32();
            if (id == HK_EXIT || id == HK_STOP) { Close(); return; }
        }
        base.WndProc(ref m);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Total pages count
    // ─────────────────────────────────────────────────────────────────────────
    int TotalPages => 8 + customPages.Count;

    // ─────────────────────────────────────────────────────────────────────────
    //  Navigate
    // ─────────────────────────────────────────────────────────────────────────
    void NavigatePage(int delta)
    {
        if (installing) return;
        page = Math.Clamp(page + delta, 0, TotalPages - 1);
        // page 6 = fake install
        if (page == 6 && delta > 0) { elapsed = 0; installing = true; progress.Value = 0; installTimer.Start(); }
        ShowPage();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ShowPage
    // ─────────────────────────────────────────────────────────────────────────
    void ShowPage()
    {
        content.Controls.Clear();

        bool isInstalling = page == 6;
        bool isFinal      = page == TotalPages - 1;

        progress.Visible    = isInstalling;
        progressTxt.Visible = isInstalling;
        btnBack.Enabled     = page > 0 && !isInstalling && !isFinal;
        btnNext.Enabled     = !isInstalling;
        btnCancel.Enabled   = !isFinal;
        btnNext.Text        = isFinal ? "Готово" : "Далее >";
        if (isFinal) { btnNext.Click -= (_, _) => NavigatePage(+1); btnNext.Click += (_, _) => Close(); }

        // Left sidebar
        var steps = new[] { "0. Добро пожаловать", "1. Интернет", "2. Персональные данные", "3. Сясь кран", "4. Компоненты", "5. Новости", "6. Установка", "7. Готово" };
        var sidebar = new System.Text.StringBuilder();
        sidebar.AppendLine("МАСТЕР УСТАНОВКИ\n");
        for (int i = 0; i < Math.Min(steps.Length, 8); i++)
            sidebar.AppendLine((i == Math.Min(page, 7) ? "► " : "  ") + steps[i]);
        if (customPages.Count > 0)
        {
            sidebar.AppendLine("\nПОЛЬЗОВАТЕЛЬСКИЕ:");
            for (int i = 0; i < customPages.Count; i++)
                sidebar.AppendLine((8 + i == page ? "► " : "  ") + customPages[i].Title);
        }
        sidebar.AppendLine("\nДИАГНОСТИКА:");
        sidebar.AppendLine("Гусь: найден");
        sidebar.AppendLine("Тараканы: найдены");
        sidebar.AppendLine("Workrave: моргает");
        sidebar.AppendLine("Генсуха: онлайн");
        sidebar.AppendLine("Вязанка: 97%");
        leftInfo.Text = sidebar.ToString();

        // Titles for built-in pages
        string[] builtinTitles = {
            "Добро пожаловать в CutVPN",
            "Параметры Интернета для локальной сети",
            "Персональные предложения",
            "Свойства: сясь кран",
            "Компоненты CutVPN",
            "Новости Чебурнета",
            "Установка CutVPN",
            "Установка завершена"
        };

        if (page < 8)
        {
            titleLbl.Text    = builtinTitles[page];
            subtitleLbl.Text = page == 6
                ? "Фейковая установка — 2 мин 30 сек. Реальная установка компонентов — по завершении."
                : "Система очень серьёзно относится к происходящему.";
        }
        else
        {
            var cp = customPages[page - 8];
            titleLbl.Text    = cp.Title;
            subtitleLbl.Text = cp.IsError ? "⚠ Ошибка системы Чебурнета" : "Пользовательская страница";
        }

        var p = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 560,
            Padding   = new Padding(22),
            BackColor = Color.FromArgb(242, 242, 242)
        };
        content.Controls.Add(p);

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
    //  Helper: label
    // ─────────────────────────────────────────────────────────────────────────
    static Label Lbl(string text, int x, int y, int w, int h, float size = 10f, FontStyle fs = FontStyle.Regular)
        => new() { Text = text, Location = new Point(x, y), Size = new Size(w, h), Font = new Font("Tahoma", size, fs) };

    static GroupBox Grp(string text, int x, int y, int w, int h)
        => new() { Text = text, Location = new Point(x, y), Size = new Size(w, h), Font = new Font("Tahoma", 9F) };

    // ─────────────────────────────────────────────────────────────────────────
    //  PAGE 0 – Welcome
    // ─────────────────────────────────────────────────────────────────────────
    void PageWelcome(Panel p)
    {
        p.Controls.Add(Lbl("Вас приветствует мастер шиттинга Чебурнета", 18, 18, 840, 44, 17, FontStyle.Bold));
        p.Controls.Add(Lbl("CutVPN подготовит вашу систему к обычному интернету Чебурнета.", 18, 72, 840, 32, 11));
        p.Controls.Add(Lbl("ГЕНСУХА одобрила этот мастер установки.", 18, 102, 840, 22, 10, FontStyle.Bold));

        var g = Grp("Что будет установлено", 18, 138, 510, 220);
        g.Controls.Add(Lbl(
            "☑  Desktop Goose  — ПРОДАМ ГУСЯ (КУПИТЬ)\n" +
            "☑  Cockroach on Desktop  — уничтожение клопов\n" +
            "☑  Workrave  — улучшение зрения не вставая\n" +
            "☑  CutVPN  — трей-приложение\n" +
            "☑  Локальный агент  — порт 8765\n" +
            "☐  TELEMAX  — шутка (ничего не устанавливается)",
            14, 22, 480, 180, 11));
        p.Controls.Add(g);

        var news = Grp("СРОЧНЫЕ НОВОСТИ ЧЕБУРНЕТА", 548, 138, 300, 220);
        news.Controls.Add(Lbl(
            "ВЯЗАНКА СНОВА БЫЛА ЗАМЕЧЕНА\nРЯДОМ С ГЕНСУХОЙ.\n\n" +
            "ГУСЬ ОТКАЗАЛСЯ ДАВАТЬ\nКОММЕНТАРИИ.\n\n" +
            "Клопы требуют лицензию.",
            14, 22, 270, 185, 10, FontStyle.Bold));
        p.Controls.Add(news);

        p.Controls.Add(Lbl("Нажмите «Далее» для продолжения. Или не нажимайте — система всё равно уже запустилась.", 18, 378, 840, 28, 9, FontStyle.Italic));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PAGE 1 – Internet
    // ─────────────────────────────────────────────────────────────────────────
    void PageInternet(Panel p)
    {
        p.Controls.Add(Lbl("Параметры Интернета для локальной сети", 18, 18, 840, 36, 14, FontStyle.Bold));
        p.Controls.Add(Lbl("Выберите способ настройки. Использование автоматической настройки Чебурнета рекомендуется.", 18, 60, 840, 30));

        var g = Grp("Автоматическая настройка", 18, 104, 820, 170);
        g.Controls.Add(new CheckBox { Text = "Автоматическое определение прокси-сервера (рекомендуется)", Location = new Point(18, 28), AutoSize = true, Checked = true });
        g.Controls.Add(new CheckBox { Text = "Использовать сценарий автоматической настройки", Location = new Point(18, 62), AutoSize = true });
        g.Controls.Add(new Label    { Text = "Адрес:", Location = new Point(18, 102), AutoSize = true });
        g.Controls.Add(new TextBox  { Location = new Point(76, 98), Width = 580, Text = "http://proxy.cheburetnet.local/auto.pac" });
        p.Controls.Add(g);

        p.Controls.Add(new CheckBox { Text = "Ручная настройка прокси-сервера", Location = new Point(20, 296), AutoSize = true });

        var g2 = Grp("Результат диагностики сети", 18, 330, 820, 130);
        g2.Controls.Add(Lbl(
            "Сеть успешно распознана как: ЛОКАЛЬНАЯ СЕТЬ ГУСЯ\n" +
            "Прокси: найден по праздникам\n" +
            "Гусь: присутствует на рабочей частоте\n" +
            "Клопы: требуют отдельную лицензию\n" +
            "Чебурнет: подключён (гарантий нет)",
            14, 22, 790, 100, 10));
        p.Controls.Add(g2);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PAGE 2 – Personal
    // ─────────────────────────────────────────────────────────────────────────
    void PagePersonal(Panel p)
    {
        p.Controls.Add(Lbl("Персональные предложения", 18, 18, 840, 36, 14, FontStyle.Bold));
        p.Controls.Add(Lbl(
            "Георгий просит вас предоставить все персональные данные.\n" +
            "Вся информация будет храниться до 10380 дней.", 18, 60, 840, 48, 10));

        var g = Grp("Ваши данные", 18, 118, 820, 200);
        g.Controls.Add(Lbl("Кто вы по национальности?", 14, 28, 200, 24));
        txNationality.Location = new Point(230, 25); g.Controls.Add(txNationality);
        g.Controls.Add(Lbl("Количество детей в семье", 14, 68, 200, 24));
        numChildren.Location = new Point(230, 64); g.Controls.Add(numChildren);
        g.Controls.Add(Lbl("Ваш любимый гусь?", 14, 108, 200, 24));
        cmbGoose.Location = new Point(230, 104); g.Controls.Add(cmbGoose);
        g.Controls.Add(Lbl("Улучшение зрения, не вставая из-за ПК", 14, 148, 600, 24, 10, FontStyle.Bold));
        p.Controls.Add(g);

        var g2 = Grp("Правовая информация", 18, 330, 820, 110);
        g2.Controls.Add(Lbl(
            "Кнопка «Далее» автоматически означает согласие со всем вышеизложенным,\n" +
            "включая то, что вы не читали. Данные хранятся 10380 дней или до потопа.",
            14, 22, 790, 78, 9, FontStyle.Italic));
        p.Controls.Add(g2);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PAGE 3 – Crane
    // ─────────────────────────────────────────────────────────────────────────
    void PageCrane(Panel p)
    {
        p.Controls.Add(Lbl("Свойства: сясь кран", 18, 18, 840, 36, 14, FontStyle.Bold));
        p.Controls.Add(Lbl("ваш sаmsung кран:\nНетрадиционный моне писа нахуй в VBE Miniport — Standard PCI Graphics Adapter (VGA).", 18, 62, 840, 46));

        var g = Grp("Настройки крана", 18, 120, 820, 250);
        g.Controls.Add(Lbl("Ваш гендер", 14, 30, 160, 25));
        cmbGender.Location = new Point(190, 27); g.Controls.Add(cmbGender);
        g.Controls.Add(Lbl("Область империи", 14, 76, 160, 25));
        cmbEmpire.Location = new Point(190, 72); g.Controls.Add(cmbEmpire);
        g.Controls.Add(Lbl("Мощность крана: мало  ←→  дохуя", 14, 120, 340, 25));
        trackCrane.Location = new Point(370, 116); g.Controls.Add(trackCrane);
        g.Controls.Add(Lbl("Статус: кран существует.", 14, 172, 600, 25, 10, FontStyle.Bold));
        g.Controls.Add(Lbl("Дополнительно: Samsung кран версии 3.14.ГУСЬ обнаружен и проверен.", 14, 206, 600, 25, 9, FontStyle.Italic));
        p.Controls.Add(g);

        var g2 = Grp("Дополнительно", 18, 386, 820, 70);
        g2.Controls.Add(Lbl("VBE Miniport — Standard PCI Graphics Adapter (VGA). Кран работает. Претензий нет.", 14, 22, 790, 40, 9));
        p.Controls.Add(g2);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PAGE 4 – Components
    // ─────────────────────────────────────────────────────────────────────────
    void PageComponents(Panel p)
    {
        p.Controls.Add(Lbl("Выбор компонентов CutVPN", 18, 18, 840, 36, 14, FontStyle.Bold));
        p.Controls.Add(Lbl("По умолчанию выбрано всё. Каждый компонент устанавливается явно, видимо, из папки payload\\.", 18, 60, 840, 30));

        // Goose
        var gGoose = Grp("Desktop Goose", 18, 100, 820, 82);
        gGoose.Controls.Add(Lbl("«ПРОДАМ ГУСЯ» — один штука. Состояние: бегает. Комплектация: клюв, лапы, гусь.", 14, 18, 600, 24, 10, FontStyle.Bold));
        chkGoose.Location = new Point(14, 48); gGoose.Controls.Add(chkGoose);
        var btnBuy = new Button { Text = "КУПИТЬ", Location = new Point(688, 40), Size = new Size(110, 28) };
        btnBuy.Click += (_, _) => MessageBox.Show("Гусь продан.\nДоставка: не предусмотрена.", "CutVPN Market", MessageBoxButtons.OK, MessageBoxIcon.Information);
        gGoose.Controls.Add(btnBuy);
        p.Controls.Add(gGoose);

        // Cockroach
        var gCock = Grp("Cockroach on Desktop", 18, 194, 820, 78);
        gCock.Controls.Add(Lbl("Эксклюзивная услуга: уничтожение клопов из дома (не гарантируется).", 14, 18, 600, 24, 10, FontStyle.Bold));
        chkCockroach.Location = new Point(14, 46); gCock.Controls.Add(chkCockroach);
        p.Controls.Add(gCock);

        // Workrave
        var gWork = Grp("Workrave", 18, 284, 820, 78);
        gWork.Controls.Add(Lbl("Улучшение зрения, не вставая из-за ПК. Рекомендовано генсухой.", 14, 18, 600, 24, 10, FontStyle.Bold));
        chkWorkrave.Location = new Point(14, 46); gWork.Controls.Add(chkWorkrave);
        p.Controls.Add(gWork);

        // Startup
        chkStartup.Location = new Point(22, 376); p.Controls.Add(chkStartup);

        // Telemax
        var gTele = Grp("TELEMAX — только шутка", 18, 406, 820, 88);
        gTele.Controls.Add(Lbl("⚠ Реальный клиент Telemax НЕ устанавливается.\nТолько создаётся файл telemax-joke.txt с текстом.", 14, 16, 700, 38, 9, FontStyle.Italic));
        chkTelemax.Location = new Point(14, 56); gTele.Controls.Add(chkTelemax);
        p.Controls.Add(gTele);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PAGE 5 – News
    // ─────────────────────────────────────────────────────────────────────────
    void PageNews(Panel p)
    {
        p.Controls.Add(Lbl("НОВОСТИ ЧЕБУРНЕТА", 18, 18, 840, 40, 16, FontStyle.Bold));
        p.Controls.Add(Lbl("Выпуск № " + new Random().Next(1000, 9999) + " · Редакция: одного гуся", 18, 60, 840, 22, 9, FontStyle.Italic));

        var sb = new System.Text.StringBuilder();
        foreach (var n in BreakingNews) sb.AppendLine("• " + n + "\n");
        p.Controls.Add(Lbl(sb.ToString(), 18, 92, 840, 380, 11, FontStyle.Bold));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PAGE 6 – Installing
    // ─────────────────────────────────────────────────────────────────────────
    void PageInstalling(Panel p)
    {
        p.Controls.Add(Lbl("Файлы копируются. Жизненно важные решения принимаются.", 18, 18, 840, 40, 14, FontStyle.Bold));
        p.Controls.Add(Lbl("Реальная установка компонентов из папки payload\\ происходит автоматически в ключевые моменты.", 18, 64, 840, 30, 11));

        var g1 = Grp("Компоненты", 18, 106, 380, 230);
        g1.Controls.Add(Lbl(
            "  CutVPN.exe\n  config.json\n  agent.json\n  installed-components.txt\n" +
            (chkGoose.Checked     ? "► Desktop Goose\n" : "") +
            (chkCockroach.Checked ? "► Cockroach on Desktop\n" : "") +
            (chkWorkrave.Checked  ? "► Workrave\n" : "") +
            (chkTelemax.Checked   ? "  TELEMAX joke (txt)\n" : ""),
            10, 22, 360, 195, 11));
        p.Controls.Add(g1);

        var g2 = Grp("Служебные операции", 414, 106, 424, 230);
        g2.Controls.Add(Lbl(
            "☑ Проверка лицензии\n☑ Согласование с генсухой\n☑ Вязанка загружена\n" +
            "☑ OSEMENIT.Bimbim проверен\n☑ Гусь найден\n☑ Тараканы зарегистрированы\n☑ Чебурнет активирован",
            10, 22, 400, 195, 11));
        p.Controls.Add(g2);

        p.Controls.Add(Lbl("Не закрывайте мастер: он делает вид, что всё под контролем.", 18, 350, 840, 28, 10, FontStyle.Italic));
        progressTxt.Text = $"{FakeStatuses[(elapsed / 10) % FakeStatuses.Length]}   Осталось ~{Math.Max(0, INSTALL_SECONDS - elapsed)} сек.";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PAGE 7 – Finish
    // ─────────────────────────────────────────────────────────────────────────
    void PageFinish(Panel p)
    {
        p.Controls.Add(Lbl("УСТАНОВКА ЗАВЕРШЕНА", 18, 18, 840, 46, 18, FontStyle.Bold));
        p.Controls.Add(Lbl($"CutVPN сохранён в:\n{Paths.Root}", 18, 72, 840, 44, 11));

        var g = Grp("Итоги", 18, 126, 840, 220);
        g.Controls.Add(Lbl(
            $"Desktop Goose:      {(chkGoose.Checked     ? "✓ установлен" : "— пропущен")}\n" +
            $"Cockroach:          {(chkCockroach.Checked ? "✓ установлен" : "— пропущен")}\n" +
            $"Workrave:           {(chkWorkrave.Checked  ? "✓ установлен" : "— пропущен")}\n" +
            $"Автозапуск CutVPN:  {(chkStartup.Checked  ? "✓ включён"    : "— выключен")}\n" +
            $"TELEMAX:            {(chkTelemax.Checked   ? "✓ шутка отмечена (ничего не установлено)" : "— не отмечена")}",
            14, 22, 810, 185, 11));
        p.Controls.Add(g);

        p.Controls.Add(Lbl(
            $"Агент запущен: 127.0.0.1:{8765}   •   Telegram-бот: настройте BOT_TOKEN в переменных окружения\n" +
            "Веб-конструктор страниц: откройте pages/index.html в браузере для добавления своих страниц.",
            18, 358, 840, 48, 10, FontStyle.Bold));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Custom page (from web constructor)
    // ─────────────────────────────────────────────────────────────────────────
    static void PageCustom(Panel p, CustomPage cp)
    {
        if (ColorTranslator.FromHtml(cp.Color) is Color bg) p.BackColor = bg;
        p.Controls.Add(Lbl(cp.Title, 18, 18, 840, 40, 14, FontStyle.Bold));
        if (cp.IsError)
            p.Controls.Add(Lbl("⚠ ОШИБКА СИСТЕМЫ ЧЕБУРНЕТА", 18, 66, 840, 30, 12, FontStyle.Bold));
        p.Controls.Add(Lbl(cp.Body, 18, cp.IsError ? 106 : 72, 840, 380, 11));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Component launcher
    // ─────────────────────────────────────────────────────────────────────────
    void LaunchComponent(string key)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "payload");
        string[] candidates = key switch
        {
            "goose"     => new[] { "DesktopGoose.Setup.exe", "DesktopGoose.exe", "DesktopGoose.msi" },
            "cockroach" => new[] { "CockroachOnDesktop.exe", "Cockroach.Setup.exe", "Cockroach.msi" },
            "workrave"  => new[] { "workrave-setup.exe", "Workrave.Setup.exe", "Workrave.exe", "Workrave.msi" },
            _           => Array.Empty<string>()
        };
        foreach (var f in candidates)
        {
            var full = Path.Combine(dir, f);
            if (!File.Exists(full)) continue;
            try
            {
                Process.Start(new ProcessStartInfo(full)
                {
                    UseShellExecute  = true,
                    WorkingDirectory = dir,
                    Verb             = "open"
                });
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось запустить {f}:\n{ex.Message}",
                    "CutVPN Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        // Компонент не найден в payload — уведомить
        MessageBox.Show(
            $"Компонент «{key}» не найден в папке payload\\.\n" +
            "Поместите установщик туда и повторите установку.",
            "CutVPN Setup — компонент не найден",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Install tick
    // ─────────────────────────────────────────────────────────────────────────
    void TickInstall()
    {
        elapsed++;
        progress.Value   = Math.Min(elapsed, INSTALL_SECONDS);
        progressTxt.Text = $"{FakeStatuses[(elapsed / 10) % FakeStatuses.Length]}   Осталось ~{Math.Max(0, INSTALL_SECONDS - elapsed)} сек.";

        // Запуск компонентов в середине фейковой установки
        if (elapsed == 15  && chkGoose.Checked)     LaunchComponent("goose");
        if (elapsed == 60  && chkCockroach.Checked) LaunchComponent("cockroach");
        if (elapsed == 105 && chkWorkrave.Checked)  LaunchComponent("workrave");

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

        // config.json
        var cfg = new InstallConfig
        {
            Goose       = chkGoose.Checked,
            Cockroach   = chkCockroach.Checked,
            Workrave    = chkWorkrave.Checked,
            Startup     = chkStartup.Checked,
            Visuals     = true,
            TelemaxJoke = chkTelemax.Checked,
            Nationality = txNationality.Text,
            Children    = (int)numChildren.Value,
            FavoriteGoose = cmbGoose.Text,
            Empire      = cmbEmpire.Text,
        };
        File.WriteAllText(Paths.Config, JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));

        // agent.json
        File.WriteAllText(Paths.AgentJson, JsonSerializer.Serialize(new
        {
            bind     = "127.0.0.1",
            port     = 8765,
            auth     = "SET_LOCAL_SECRET",
            commands = new[]
            {
                "status","visuals_on","visuals_off","screenshot",
                "restart","volume","random_error","wallpaper_set",
                "sound_play","video_play","uninstall"
            }
        }, new JsonSerializerOptions { WriteIndented = true }));

        // telemax joke
        if (chkTelemax.Checked)
            File.WriteAllText(Paths.Joke,
                "Поздравляем. Вас почти установили. Но нет.\r\n" +
                "TELEMAX — шутливый пункт. Реальный клиент не устанавливается.\r\n");

        // startup
        if (chkStartup.Checked)
            File.WriteAllText(Paths.StartupCmd,
                $"@echo off\r\nstart \"\" \"{Paths.AppExe}\" --installed\r\n");
        else if (File.Exists(Paths.StartupCmd))
            File.Delete(Paths.StartupCmd);

        // installed-components.txt
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# CutVPN installed-components");
        sb.AppendLine($"# {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        if (chkGoose.Checked)     sb.AppendLine("Goose=installed");
        if (chkCockroach.Checked) sb.AppendLine("Cockroach=installed");
        if (chkWorkrave.Checked)  sb.AppendLine("Workrave=installed");
        if (chkStartup.Checked)   sb.AppendLine("Startup=enabled");
        if (chkTelemax.Checked)   sb.AppendLine("TelemaxJoke=noted");
        File.WriteAllText(Paths.Installed, sb.ToString());
    }
}
