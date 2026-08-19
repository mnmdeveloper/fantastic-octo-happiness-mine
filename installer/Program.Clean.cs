using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.Json;
using Microsoft.Win32;
using System.Windows.Forms;

namespace CutVPN.Setup;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.Run(new SetupWizard());
    }
}

internal sealed class SetupConfig
{
    public bool Goose { get; set; } = true;
    public bool Cockroach { get; set; } = true;
    public bool Workrave { get; set; } = true;
    public bool Visuals { get; set; } = true;
    public bool Startup { get; set; } = true;
    public string Nationality { get; set; } = "Чебурек";
    public int Children { get; set; } = 1;
    public string Empire { get; set; } = "Империя Чебурнета";
}

internal sealed class SetupWizard : Form
{
    private readonly Label pageTitle = new();
    private readonly Label pageDescription = new();
    private readonly Panel pageHost = new();
    private readonly ProgressBar progress = new();
    private readonly Label progressText = new();
    private readonly Button back = new() { Text = "< Назад" };
    private readonly Button next = new() { Text = "Далее >" };
    private readonly Button cancel = new() { Text = "Отмена" };
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 160 };
    private readonly CheckBox goose = new() { Text = "Desktop Goose — важнейший сетевой гусь", AutoSize = true, Checked = true };
    private readonly CheckBox cockroach = new() { Text = "Cockroach on Desktop — анти-клоповый режим", AutoSize = true, Checked = true };
    private readonly CheckBox workrave = new() { Text = "Workrave — улучшение зрения не вставая из-за ПК", AutoSize = true, Checked = true };
    private readonly CheckBox visuals = new() { Text = "CutVPN Prank Visuals — рекомендуется", AutoSize = true, Checked = true };
    private readonly CheckBox startup = new() { Text = "Запускать CutVPN при входе в Windows", AutoSize = true, Checked = true };
    private readonly TextBox nationality = new() { Text = "Чебурек", Width = 280 };
    private readonly NumericUpDown children = new() { Minimum = 0, Maximum = 99, Value = 1, Width = 90 };
    private readonly ComboBox empire = new() { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TrackBar absurdity = new() { Minimum = 0, Maximum = 100, Value = 65, TickFrequency = 10, Width = 320 };
    private int page;
    private int fakeProgress;
    private bool installing;
    private readonly string[] statusPool =
    {
        "Упорядочиваем вязанку...",
        "Согласовываем с генсухой...",
        "Ищем OSEMENIT.Bimbim...",
        "Проверяем, где гусь...",
        "Ускоряем Чебурнет...",
        "Проверяем клопов на наличие лицензии...",
        "Устанавливаем Framework по доению коровы...",
        "Настраиваем абсолютную серьёзность..."
    };

    public SetupWizard()
    {
        Text = "Мастер шиттинга Чебурнета — CutVPN Setup";
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        BackColor = Color.FromArgb(192, 192, 192);
        Font = new Font("Tahoma", 9F);
        KeyPreview = true;
        empire.Items.AddRange(new object[] { "Империя Чебурнета", "Гусландия", "Вязаночная область", "Территория генсухи" });
        empire.SelectedIndex = 0;

        BuildChrome();
        timer.Tick += (_, _) => InstallTick();
        KeyDown += OnKeyDown;
        ShowPage();
    }

    private void BuildChrome()
    {
        var top = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Navy };
        top.Controls.Add(new Label { Text = "Мастер шиттинга Чебурнета", ForeColor = Color.White, Font = new Font("Tahoma", 15F, FontStyle.Bold), Dock = DockStyle.Left, Padding = new Padding(16, 12, 0, 0), AutoSize = true });
        var close = new Button { Text = "X", Dock = DockStyle.Right, Width = 44, Height = 36, FlatStyle = FlatStyle.Flat, BackColor = Color.Navy, ForeColor = Color.White, Font = new Font("Tahoma", 11F, FontStyle.Bold) };
        close.Click += (_, _) => Close();
        top.Controls.Add(close);
        Controls.Add(top);

        var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(14, 10, 14, 0) };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        Controls.Add(main);

        var left = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(210, 210, 210), BorderStyle = BorderStyle.Fixed3D, AutoScroll = true };
        left.Controls.Add(new RetroPoster { Dock = DockStyle.Top, Height = 220 });
        left.Controls.Add(new Label { Dock = DockStyle.Top, Height = 165, Padding = new Padding(18, 14, 8, 8), Text = "CutVPN 98ΞP-ЯК\n\nСистема обнаружила:\n• 1 гусь\n• 47 тараканов\n• 0 стабильных решений\n• 1 вязанку\n• неопределённое количество генсухи", Font = new Font("Tahoma", 9F) });
        left.Controls.Add(new Label { Dock = DockStyle.Top, Height = 70, Padding = new Padding(18, 10, 8, 8), Text = "Состояние системы: работает\nСтепень шиттинга: ВЫСОКАЯ", ForeColor = Color.Navy, Font = new Font("Tahoma", 9F, FontStyle.Bold) });
        main.Controls.Add(left, 0, 0);

        var right = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.Fixed3D };
        main.Controls.Add(right, 1, 0);

        var header = new Panel { Dock = DockStyle.Top, Height = 126, BackColor = Color.White, Padding = new Padding(24, 16, 20, 10) };
        pageTitle.Dock = DockStyle.Top; pageTitle.Height = 34; pageTitle.Font = new Font("Tahoma", 17F, FontStyle.Bold); pageTitle.ForeColor = Color.Navy;
        pageDescription.Dock = DockStyle.Top; pageDescription.Height = 70; pageDescription.Font = new Font("Tahoma", 10F);
        header.Controls.Add(pageDescription); header.Controls.Add(pageTitle); right.Controls.Add(header);

        pageHost.Dock = DockStyle.Fill; pageHost.BackColor = Color.FromArgb(242, 242, 242); pageHost.BorderStyle = BorderStyle.Fixed3D; pageHost.AutoScroll = true; right.Controls.Add(pageHost);

        var prog = new Panel { Dock = DockStyle.Bottom, Height = 58, Padding = new Padding(22, 8, 22, 8), BackColor = Color.White };
        progress.Dock = DockStyle.Top; progress.Height = 18; progress.Minimum = 0; progress.Maximum = 100; progress.Visible = false;
        progressText.Dock = DockStyle.Bottom; progressText.Height = 22; progressText.Visible = false; progressText.ForeColor = Color.DimGray;
        prog.Controls.Add(progressText); prog.Controls.Add(progress); right.Controls.Add(prog);

        var buttons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Padding = new Padding(0, 12, 0, 0), BackColor = Color.FromArgb(212, 212, 212) };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        buttons.Controls.Add(new Label { Text = "Win+U / Esc — выйти • Ctrl+Shift+G — аварийный стоп", ForeColor = Color.DimGray, Dock = DockStyle.Fill, Padding = new Padding(4, 5, 0, 0) }, 0, 0);
        foreach (var b in new[] { back, next, cancel }) { b.Dock = DockStyle.Fill; b.Margin = new Padding(4, 0, 4, 10); buttons.Controls.Add(b); }
        main.Controls.Add(buttons, 0, 1); main.SetColumnSpan(buttons, 2);
        back.Click += (_, _) => Navigate(-1); next.Click += (_, _) => Navigate(1); cancel.Click += (_, _) => Close();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape || (e.KeyCode == Keys.U && e.Modifiers == Keys.Windows)) Close();
        if (e.KeyCode == Keys.G && e.Control && e.Shift) { Close(); e.SuppressKeyPress = true; }
    }

    private void Navigate(int delta)
    {
        if (installing) return;
        page = Math.Clamp(page + delta, 0, 7);
        if (page == 6) StartInstallation(); else ShowPage();
    }

    private void ShowPage()
    {
        pageHost.Controls.Clear();
        progress.Visible = page == 6; progressText.Visible = page == 6;
        back.Enabled = page > 0 && page < 6; next.Enabled = page != 6; cancel.Enabled = page < 7;
        next.Text = page == 7 ? "Готово" : "Далее >";
        pageTitle.Text = new[] { "Добро пожаловать в CutVPN", "Параметры Интернета для локальной сети", "Персональные предложения", "Свойства: сясь кран", "Выбор обязательных компонентов", "Подтверждение системной безопасности", "Установка CutVPN", "Установка завершена" }[page];
        pageDescription.Text = new[] { "Добро пожаловать в самый серьёзный сетевой мастер на этой стороне Чебурнета.", "Выберите способ настройки прокси-сервера. Автоматический вариант рекомендуется гусём.", "Несколько абсолютно необходимых персональных предложений для комфортной жизни за ПК.", "Настройка устройства «сясь кран». Большинство параметров не имеет смысла — значит, они обязательны.", "Выберите компоненты, которые будут подготовлены вместе с CutVPN.", "Последняя проверка безопасности. Все предупреждения абсолютно научные.", "Файлы копируются. Генсуха согласовывает вязанку.", "Готово. Даже гусь признал завершение процедуры." }[page];
        switch (page) { case 0: Welcome(); break; case 1: Internet(); break; case 2: Personal(); break; case 3: Crane(); break; case 4: Components(); break; case 5: Security(); break; case 6: Installation(); break; case 7: Finish(); break; }
    }

    private Panel Surface() { var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28), BackColor = Color.FromArgb(242, 242, 242), AutoScroll = true }; pageHost.Controls.Add(p); return p; }
    private void AddText(Panel p, string text, int y, int height = 100, int size = 10) => p.Controls.Add(new Label { Text = text, Location = new Point(18, y), Size = new Size(Math.Max(500, pageHost.ClientSize.Width - 60), height), Font = new Font("Tahoma", size) });

    private void Welcome() { var p = Surface(); AddText(p, "CutVPN — установка сетевого продукта\n\nГЕНСУХА одобрила этот мастер.", 20, 110, 16); AddText(p, "В комплекте подготовлены:\n\n• гусь\n• тараканы\n• Workrave\n• пранк-визуалы\n• вязанка\n• осеменение по необходимости\n• несколько очень сомнительных настроек", 145, 220, 11); p.Controls.Add(new RetroPoster { Location = new Point(560, 36), Size = new Size(245, 220) }); }
    private void Internet() { var p = Surface(); AddText(p, "Выберите способ настройки прокси-сервера.\n\nИспользование автоматической настройки рекомендуется системой, гусём и неизвестным сетевым администратором.", 18, 90); var g = new GroupBox { Text = "Автоматическая настройка", Location = new Point(18, 120), Size = new Size(760, 150) }; g.Controls.Add(new CheckBox { Text = "Автоматическое определение прокси-сервера (рекомендуется)", Location = new Point(18, 28), AutoSize = true, Checked = true }); g.Controls.Add(new CheckBox { Text = "Использовать сценарий автоматической настройки", Location = new Point(18, 60), AutoSize = true }); g.Controls.Add(new Label { Text = "Адрес:", Location = new Point(18, 98), AutoSize = true }); g.Controls.Add(new TextBox { Location = new Point(76, 94), Width = 500, Text = "http://proxy.cheburetnet.local/auto.pac" }); p.Controls.Add(g); p.Controls.Add(new CheckBox { Text = "Ручная настройка прокси-сервера", Location = new Point(18, 290), AutoSize = true }); AddText(p, "Сеть успешно распознана как: ЛОКАЛЬНАЯ СЕТЬ ГУСЯ", 330, 40, 10); }
    private void Personal() { var p = Surface(); AddText(p, "Георгий просит вас предоставить все персональные данные для их обработки и хранения. Вся информация будет храниться до 10380 дней.", 18, 70, 10); p.Controls.Add(new Label { Text = "Кто вы по национальности?", Location = new Point(18, 110), AutoSize = true }); nationality.Location = new Point(230, 106); p.Controls.Add(nationality); p.Controls.Add(new Label { Text = "Количество детей в семье", Location = new Point(18, 158), AutoSize = true }); children.Location = new Point(230, 154); p.Controls.Add(children); p.Controls.Add(new Label { Text = "Ваш любимый гусь?", Location = new Point(18, 206), AutoSize = true }); p.Controls.Add(new TextBox { Text = "Гусь", Location = new Point(230, 202), Width = 280 }); p.Controls.Add(new Label { Text = "Кнопка «Далее» автоматически означает согласие со всем вышеизложенным.", Location = new Point(18, 260), AutoSize = true, ForeColor = Color.Maroon }); }
    private void Crane() { var p = Surface(); AddText(p, "ваш sаmsung кран:\n\nНетрадиционный моне писа нахуй в VBE Miniport - Standard PCI Graphics Adapter (VGA).", 18, 90, 12); p.Controls.Add(new Label { Text = "Ваш гендер", Location = new Point(18, 120), AutoSize = true }); var gender = new ComboBox { Location = new Point(18, 146), Width = 310, DropDownStyle = ComboBoxStyle.DropDownList }; gender.Items.AddRange(new object[] { "АНАНАС SPIR PRO(много)", "Гусь", "Вязанка", "Работает кран" }); gender.SelectedIndex = 0; p.Controls.Add(gender); p.Controls.Add(new Label { Text = "Область империи", Location = new Point(390, 120), AutoSize = true }); empire.Location = new Point(390, 146); p.Controls.Add(empire); p.Controls.Add(new Label { Text = "Область империи: мало → дохуя", Location = new Point(390, 205), AutoSize = true }); absurdity.Location = new Point(390, 230); p.Controls.Add(absurdity); p.Controls.Add(new Label { Text = "Проверка крана завершена: кран существует.", Location = new Point(18, 310), AutoSize = true, ForeColor = Color.DarkGreen }); }
    private void Components() { var p = Surface(); AddText(p, "Установка компонентов CutVPN. Все пункты видимы и выбираются здесь.", 18, 60, 12); goose.Location = new Point(36, 82); cockroach.Location = new Point(36, 125); workrave.Location = new Point(36, 168); visuals.Location = new Point(36, 211); startup.Location = new Point(36, 254); foreach (var c in new Control[] { goose, cockroach, workrave, visuals, startup }) p.Controls.Add(c); AddText(p, "Workrave: «улучшение зрения, не вставая из-за ПК»\nCockroach: «уничтожение клопов из дома»\nGoose: «ПРОДАМ ГУСЯ» — кнопка только одна: КУПИТЬ", 305, 100, 10); }
    private void Security() { var p = Surface(); AddText(p, "☑ Обновление Framework по доению коровы\n☑ Проверка OSEMENIT.Bimbim\n☑ Согласование вязанки с генсухой\n☑ Проверка наличия гуся\n☑ Сертификация тараканов\n☑ Workrave должен научить пользователя моргать", 28, 220, 11); AddText(p, "Нажмите «Далее», чтобы начать установку.\nПосле этого мастер некоторое время будет очень серьёзно делать вид, что работает.", 260, 80, 10); }
    private void Installation() { var p = Surface(); AddText(p, "Файлы копируются. Жизненно важные решения принимаются.", 18, 50, 13); AddText(p, "CutVPN.exe\nprank.state\ncheburetnet.cfg\ngus.dll\nvazyanka.sys\nOSEMENIT.Bimbim", 90, 180, 11); }
    private void Finish() { var p = Surface(); AddText(p, "УСТАНОВКА ЗАВЕРШЕНА", 25, 60, 18); AddText(p, "CutVPN установлен в:\n" + InstallPaths.Root + "\n\nГусь: подготовлен\nТараканы: подготовлены\nWorkrave: подготовлен\nПранк-визуалы: " + (visuals.Checked ? "ВКЛ" : "ВЫКЛ") + "\nАвтозапуск CutVPN: " + (startup.Checked ? "ВКЛ" : "ВЫКЛ"), 95, 230, 11); AddText(p, "Нажмите «Готово», чтобы закрыть мастер.", 340, 40, 10); }

    private void StartInstallation() { fakeProgress = 0; progress.Value = 0; progressText.Text = statusPool[0]; installing = true; back.Enabled = false; cancel.Enabled = false; next.Enabled = false; ShowPage(); timer.Start(); }
    private void InstallTick() { fakeProgress = Math.Min(100, fakeProgress + 1); progress.Value = fakeProgress; progressText.Text = statusPool[(fakeProgress / 8) % statusPool.Length]; if (fakeProgress >= 100) { timer.Stop(); PerformInstall(); installing = false; page = 7; ShowPage(); next.Enabled = true; next.Text = "Готово"; } }

    private void PerformInstall()
    {
        Directory.CreateDirectory(InstallPaths.Root);
        var cfg = new SetupConfig { Goose = goose.Checked, Cockroach = cockroach.Checked, Workrave = workrave.Checked, Visuals = visuals.Checked, Startup = startup.Checked, Nationality = nationality.Text, Children = (int)children.Value, Empire = empire.Text };
        File.WriteAllText(InstallPaths.Config, JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(InstallPaths.Components, "Goose=" + cfg.Goose + Environment.NewLine + "Cockroach=" + cfg.Cockroach + Environment.NewLine + "Workrave=" + cfg.Workrave + Environment.NewLine + "Visuals=" + cfg.Visuals + Environment.NewLine + "Startup=" + cfg.Startup + Environment.NewLine);
        var current = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(current)) File.Copy(current, InstallPaths.Exe, true);
        InstallPaths.SetStartup(cfg.Startup);
        InstallPayloads();
    }

    private void InstallPayloads()
    {
        var payload = Path.Combine(AppContext.BaseDirectory, "payload");
        if (!Directory.Exists(payload)) return;
        var dest = Path.Combine(InstallPaths.Root, "payload");
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(payload)) File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
    }
}

internal static class InstallPaths
{
    public static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CutVPN");
    public static string Exe => Path.Combine(Root, "CutVPN.exe");
    public static string Config => Path.Combine(Root, "config.json");
    public static string Components => Path.Combine(Root, "installed-components.txt");
    public static void SetStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        if (key == null) return;
        if (enabled) key.SetValue("CutVPN", Exe); else key.DeleteValue("CutVPN", false);
    }
}

internal sealed class RetroPoster : Panel
{
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var bg = new LinearGradientBrush(ClientRectangle, Color.Teal, Color.SteelBlue, 25f);
        e.Graphics.FillRectangle(bg, ClientRectangle);
        e.Graphics.DrawString("WINDOWS", new Font("Tahoma", 22F, FontStyle.Bold), Brushes.White, 14, 16);
        e.Graphics.DrawString("ХУЯК XP/VISTA", new Font("Tahoma", 13F, FontStyle.Bold), Brushes.Yellow, 14, 48);
        e.Graphics.FillRectangle(Brushes.White, 14, 86, Width - 28, 112);
        e.Graphics.DrawRectangle(Pens.Navy, 14, 86, Width - 28, 112);
        e.Graphics.DrawString("ПЕРСОНАЛЬНЫЕ\nПРЕДЛОЖЕНИЯ", new Font("Tahoma", 10F, FontStyle.Bold), Brushes.Navy, 25, 100);
        e.Graphics.DrawString("Гусь подключён.\nВязанка загружена.\nОсеменение: 97%.", new Font("Tahoma", 8F), Brushes.Black, 25, 148);
    }
}
