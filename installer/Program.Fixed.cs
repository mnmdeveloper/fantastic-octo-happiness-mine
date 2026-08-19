using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Runtime.InteropServices;

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
    private const int HotkeyId = 42001;
    private const uint ModWin = 0x0008;
    private const int WmHotkey = 0x0312;

    private readonly Panel pageHost;
    private readonly Label pageTitle;
    private readonly Label pageDescription;
    private readonly Label statusLine;
    private readonly ProgressBar installProgress;
    private readonly Button backButton;
    private readonly Button nextButton;
    private readonly Button cancelButton;
    private readonly System.Windows.Forms.Timer installTimer;
    private readonly CheckBox goose;
    private readonly CheckBox cockroach;
    private readonly CheckBox workrave;
    private readonly CheckBox visuals;
    private readonly CheckBox startup;
    private readonly TextBox nationality;
    private readonly NumericUpDown children;
    private readonly ComboBox empire;
    private readonly TrackBar ridiculousness;

    private int pageIndex;
    private int fakeProgress;
    private bool installing;

    private readonly string[] statuses =
    {
        "Упорядочиваем вязанку...",
        "Согласовываем с генсухой...",
        "Ищем OSEMENIT.Bimbim...",
        "Проверяем, где гусь...",
        "Ускоряем Чебурнет...",
        "Проверяем клопов на наличие лицензии...",
        "Устанавливаем Framework по доению коровы...",
        "Генерируем абсолютно необходимые настройки..."
    };

    public SetupWizard()
    {
        Text = "Мастер шиттинга Чебурнета — CutVPN Setup";
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        BackColor = Color.FromArgb(192, 192, 192);
        KeyPreview = true;
        DoubleBuffered = true;
        Font = new Font("Tahoma", 9F);

        var titleBar = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.FromArgb(0, 0, 128) };
        Controls.Add(titleBar);
        titleBar.Controls.Add(new Label
        {
            Text = "Мастер шиттинга Чебурнета",
            ForeColor = Color.White,
            Font = new Font("Tahoma", 16F, FontStyle.Bold),
            Location = new Point(18, 13),
            AutoSize = true
        });

        var close = new Button
        {
            Text = "X",
            Font = new Font("Tahoma", 12F, FontStyle.Bold),
            Size = new Size(38, 34),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        titleBar.Controls.Add(close);
        titleBar.Resize += (_, _) => close.Location = new Point(titleBar.Width - 50, 10);
        close.Click += (_, _) => Close();

        var left = new Panel
        {
            Location = new Point(18, 72),
            Width = 280,
            Height = Screen.PrimaryScreen!.Bounds.Height - 145,
            BackColor = Color.FromArgb(210, 210, 210),
            BorderStyle = BorderStyle.Fixed3D,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
        };
        Controls.Add(left);
        left.Controls.Add(new RetroPoster { Location = new Point(16, 16), Size = new Size(246, 220) });
        left.Controls.Add(new Label
        {
            Text = "CutVPN 98ΞP-ЯК\n\nСистема обнаружила:\n• 1 гусь\n• 47 тараканов\n• 0 стабильных решений\n• 1 вязанку\n• неопределённое количество генсухи",
            Location = new Point(20, 255),
            Size = new Size(235, 180),
            Font = new Font("Tahoma", 9F)
        });
        left.Controls.Add(new Label
        {
            Text = "Состояние системы: работает\nСтепень шиттинга: высокая",
            Location = new Point(20, 460),
            Size = new Size(235, 60),
            ForeColor = Color.Navy,
            Font = new Font("Tahoma", 9F, FontStyle.Bold)
        });

        var right = new Panel
        {
            Location = new Point(316, 72),
            Width = Screen.PrimaryScreen.Bounds.Width - 334,
            Height = Screen.PrimaryScreen.Bounds.Height - 145,
            BackColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        Controls.Add(right);

        pageTitle = new Label
        {
            Location = new Point(26, 20),
            AutoSize = true,
            Font = new Font("Tahoma", 18F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 0, 128)
        };
        right.Controls.Add(pageTitle);

        pageDescription = new Label
        {
            Location = new Point(26, 60),
            Size = new Size(right.Width - 52, 80),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Tahoma", 10F)
        };
        right.Controls.Add(pageDescription);

        pageHost = new Panel
        {
            Location = new Point(18, 145),
            Width = right.Width - 36,
            Height = right.Height - 245,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.FromArgb(236, 236, 236),
            BorderStyle = BorderStyle.FixedSingle,
            AutoScroll = true
        };
        right.Controls.Add(pageHost);

        installProgress = new ProgressBar
        {
            Location = new Point(26, right.Height - 86),
            Width = right.Width - 52,
            Height = 24,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Minimum = 0,
            Maximum = 100,
            Visible = false
        };
        right.Controls.Add(installProgress);

        statusLine = new Label
        {
            Location = new Point(26, right.Height - 58),
            AutoSize = true,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            ForeColor = Color.DimGray,
            Visible = false
        };
        right.Controls.Add(statusLine);

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 68, BackColor = Color.FromArgb(212, 212, 212) };
        Controls.Add(bottom);
        backButton = new Button { Text = "< Назад", Size = new Size(110, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        nextButton = new Button { Text = "Далее >", Size = new Size(110, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        cancelButton = new Button { Text = "Отмена", Size = new Size(110, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        bottom.Controls.Add(backButton);
        bottom.Controls.Add(nextButton);
        bottom.Controls.Add(cancelButton);
        bottom.Resize += (_, _) =>
        {
            cancelButton.Location = new Point(bottom.Width - 122, 17);
            nextButton.Location = new Point(bottom.Width - 242, 17);
            backButton.Location = new Point(bottom.Width - 362, 17);
        };
        backButton.Click += (_, _) => Navigate(-1);
        nextButton.Click += (_, _) => Navigate(1);
        cancelButton.Click += (_, _) => Close();

        Controls.Add(new Label
        {
            Text = "Win+U — выйти из мастера • Ctrl+Shift+G — аварийный стоп пранка",
            Location = new Point(18, Screen.PrimaryScreen.Bounds.Height - 112),
            AutoSize = true,
            ForeColor = Color.DimGray,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        });

        goose = new CheckBox { Text = "Desktop Goose — важнейший сетевой гусь", AutoSize = true, Checked = true };
        cockroach = new CheckBox { Text = "Cockroach on Desktop — анти-клоповый режим", AutoSize = true, Checked = true };
        workrave = new CheckBox { Text = "Workrave — улучшение зрения не вставая из-за ПК", AutoSize = true, Checked = true };
        visuals = new CheckBox { Text = "CutVPN Prank Visuals — рекомендуется", AutoSize = true, Checked = true };
        startup = new CheckBox { Text = "Запускать CutVPN при входе в Windows", AutoSize = true, Checked = true };
        nationality = new TextBox { Text = "Чебурек", Width = 280 };
        children = new NumericUpDown { Minimum = 0, Maximum = 99, Value = 1, Width = 90 };
        empire = new ComboBox { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
        empire.Items.AddRange(new object[] { "Империя Чебурнета", "Гусландия", "Вязаночная область", "Территория генсухи" });
        empire.SelectedIndex = 0;
        ridiculousness = new TrackBar { Minimum = 0, Maximum = 100, Value = 60, TickFrequency = 10, Width = 360 };

        installTimer = new System.Windows.Forms.Timer { Interval = 180 };
        installTimer.Tick += (_, _) => InstallTick();

        RegisterHotKey(Handle, HotkeyId, ModWin, (int)Keys.U);
        ShowPage();
    }

    private void Navigate(int delta)
    {
        if (installing) return;
        if (pageIndex == 6 && delta > 0) return;
        pageIndex = Math.Clamp(pageIndex + delta, 0, 7);
        if (pageIndex == 6) StartInstallation();
        else ShowPage();
    }

    private void ShowPage()
    {
        pageHost.Controls.Clear();
        installProgress.Visible = pageIndex == 6;
        statusLine.Visible = pageIndex == 6;
        backButton.Enabled = pageIndex > 0 && pageIndex != 6 && pageIndex != 7;
        cancelButton.Enabled = pageIndex < 7 && !installing;
        nextButton.Enabled = pageIndex != 6;
        nextButton.Text = pageIndex == 7 ? "Готово" : "Далее >";
        pageTitle.Text = pageIndex switch
        {
            0 => "Добро пожаловать в CutVPN",
            1 => "Параметры Интернета для локальной сети",
            2 => "Персональные предложения",
            3 => "Свойства: сясь кран",
            4 => "Выбор обязательных компонентов",
            5 => "Подтверждение системной безопасности",
            6 => "Установка CutVPN",
            _ => "Установка завершена"
        };
        pageDescription.Text = pageIndex switch
        {
            0 => "Добро пожаловать в самый серьёзный сетевой мастер на этой стороне Чебурнета. Начнём абсолютно важную процедуру.",
            1 => "Выберите способ настройки прокси-сервера. Если вы не знаете, что выбрать, выберите автоматическое определение.",
            2 => "Нам нужны несколько исключительно важных сведений для дальнейшего улучшения вашей жизни у компьютера.",
            3 => "Настройка устройства «сясь кран». Большинство параметров не имеет смысла. Именно поэтому они обязательны.",
            4 => "Выберите компоненты, которые будут подготовлены вместе с CutVPN.",
            5 => "Последняя проверка безопасности перед установкой. Все предупреждения абсолютно научные.",
            6 => "Установка занимает ровно столько времени, сколько нужно для достойного старого Windows-мастера.",
            _ => "Готово. Даже гусь признал завершение процедуры."
        };

        switch (pageIndex)
        {
            case 0: BuildWelcome(); break;
            case 1: BuildInternet(); break;
            case 2: BuildPersonal(); break;
            case 3: BuildCrane(); break;
            case 4: BuildComponents(); break;
            case 5: BuildSecurity(); break;
            case 6: BuildInstall(); break;
            case 7: BuildFinish(); break;
        }
    }

    private Panel RetroSurface() => new()
    {
        Dock = DockStyle.Top,
        Height = 430,
        BackColor = Color.FromArgb(245, 245, 245),
        BorderStyle = BorderStyle.Fixed3D
    };

    private void BuildWelcome()
    {
        var p = RetroSurface();
        p.Controls.Add(new Label { Text = "CutVPN — установка сетевого продукта\n\nГЕНСУХА одобрила этот мастер.", Location = new Point(30, 25), Size = new Size(500, 90), Font = new Font("Tahoma", 15F, FontStyle.Bold) });
        p.Controls.Add(new Label { Text = "В комплекте подготовлены:\n\n• гусь\n• тараканы\n• Workrave\n• пранк-визуалы\n• вязанка\n• осеменение по необходимости\n• несколько очень сомнительных настроек", Location = new Point(34, 140), Size = new Size(500, 200), Font = new Font("Tahoma", 11F) });
        p.Controls.Add(new RetroPoster { Location = new Point(580, 40), Size = new Size(245, 220) });
        pageHost.Controls.Add(p);
    }

    private void BuildInternet()
    {
        var p = RetroSurface();
        p.Controls.Add(new Label { Text = "Выберите способ настройки прокси-сервера.\n\nИспользование автоматической настройки рекомендуется системой, гусём и одним неизвестным сетевым администратором.", Location = new Point(28, 22), Size = new Size(790, 80), Font = new Font("Tahoma", 10F) });
        var group = new GroupBox { Text = "Автоматическая настройка", Location = new Point(28, 120), Size = new Size(790, 150) };
        p.Controls.Add(group);
        group.Controls.Add(new CheckBox { Text = "☑ Автоматическое определение прокси-сервера (рекомендуется)", Location = new Point(18, 25), AutoSize = true, Checked = true });
        group.Controls.Add(new CheckBox { Text = "☐ Использовать сценарий автоматической настройки", Location = new Point(18, 60), AutoSize = true });
        group.Controls.Add(new Label { Text = "Адрес:", Location = new Point(18, 100), AutoSize = true });
        group.Controls.Add(new TextBox { Location = new Point(75, 97), Width = 500, Text = "http://proxy.cheburetnet.local/auto.pac" });
        p.Controls.Add(new CheckBox { Text = "☐ Ручная настройка прокси-сервера", Location = new Point(28, 292), AutoSize = true });
        p.Controls.Add(new Label { Text = "Сеть успешно распознана как: ЛОКАЛЬНАЯ СЕТЬ ГУСЯ", Location = new Point(28, 335), AutoSize = true, ForeColor = Color.Navy });
        pageHost.Controls.Add(p);
    }

    private void BuildPersonal()
    {
        var p = RetroSurface();
        p.Controls.Add(new Label { Text = "Георгий просит вас предоставить все персональные данные для их обработки и хранения.\nВся информация будет храниться до 10380 дней. После этого мастер забудет всё и предложит начать заново.", Location = new Point(28, 22), Size = new Size(790, 85), Font = new Font("Tahoma", 10F) });
        p.Controls.Add(new Label { Text = "Кто вы по национальности?", Location = new Point(28, 132), AutoSize = true });
        nationality.Location = new Point(230, 128); p.Controls.Add(nationality);
        p.Controls.Add(new Label { Text = "Количество детей в семье", Location = new Point(28, 181), AutoSize = true });
        children.Location = new Point(230, 177); p.Controls.Add(children);
        p.Controls.Add(new Label { Text = "Ваш любимый гусь?", Location = new Point(28, 230), AutoSize = true });
        p.Controls.Add(new TextBox { Text = "Гусь", Location = new Point(230, 226), Width = 280 });
        p.Controls.Add(new Label { Text = "Семейный тариф Чебурнета", Location = new Point(28, 280), AutoSize = true });
        var plan = new ComboBox { Location = new Point(230, 276), Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
        plan.Items.AddRange(new object[] { "1 гусь", "2 гуся", "Гусь Pro Max", "Вязанка Family" });
        plan.SelectedIndex = 0;
        p.Controls.Add(plan);
        p.Controls.Add(new Label { Text = "Кнопка «Далее» автоматически означает согласие со всем вышеизложенным.", Location = new Point(28, 335), AutoSize = true, ForeColor = Color.Maroon });
        pageHost.Controls.Add(p);
    }

    private void BuildCrane()
    {
        var p = RetroSurface();
        p.Controls.Add(new Label { Text = "ваш sаmsung кран:", Location = new Point(28, 24), AutoSize = true, Font = new Font("Tahoma", 12F, FontStyle.Bold) });
        p.Controls.Add(new Label { Text = "Нетрадиционный моне писа нахуй в VBE Miniport - Standard PCI Graphics Adapter (VGA).", Location = new Point(28, 63), Size = new Size(790, 45) });
        p.Controls.Add(new Label { Text = "Ваш гендер", Location = new Point(28, 130), AutoSize = true });
        var gender = new ComboBox { Location = new Point(28, 156), Width = 310, DropDownStyle = ComboBoxStyle.DropDownList };
        gender.Items.AddRange(new object[] { "АНАНАС SPIR PRO(много)", "Гусь", "Вязанка", "Работает кран" });
        gender.SelectedIndex = 0;
        p.Controls.Add(gender);
        p.Controls.Add(new Label { Text = "Область империи", Location = new Point(390, 130), AutoSize = true });
        empire.Location = new Point(390, 156); p.Controls.Add(empire);
        p.Controls.Add(new Label { Text = "Область империи: мало → дохуя", Location = new Point(390, 205), AutoSize = true });
        ridiculousness.Location = new Point(390, 230); p.Controls.Add(ridiculousness);
        p.Controls.Add(new Label { Text = "Проверка крана завершена: кран существует.", Location = new Point(28, 320), AutoSize = true, ForeColor = Color.DarkGreen });
        pageHost.Controls.Add(p);
    }

    private void BuildComponents()
    {
        var p = RetroSurface();
        p.Controls.Add(new Label { Text = "Установка компонентов CutVPN", Location = new Point(28, 22), AutoSize = true, Font = new Font("Tahoma", 13F, FontStyle.Bold) });
        goose.Location = new Point(40, 75); p.Controls.Add(goose);
        cockroach.Location = new Point(40, 120); p.Controls.Add(cockroach);
        workrave.Location = new Point(40, 165); p.Controls.Add(workrave);
        visuals.Location = new Point(40, 210); p.Controls.Add(visuals);
        startup.Location = new Point(40, 255); p.Controls.Add(startup);
        p.Controls.Add(new Label { Text = "Workrave: «улучшение зрения, не вставая из-за ПК»\nCockroach: «уничтожение клопов из дома»\nGoose: «ПРОДАМ ГУСЯ» — кнопка только одна: КУПИТЬ", Location = new Point(40, 305), Size = new Size(700, 70), ForeColor = Color.Navy });
        pageHost.Controls.Add(p);
    }

    private void BuildSecurity()
    {
        var p = RetroSurface();
        p.Controls.Add(new Label { Text = "Подтверждение системной безопасности", Location = new Point(28, 22), AutoSize = true, Font = new Font("Tahoma", 13F, FontStyle.Bold) });
        p.Controls.Add(new Label { Text = "☑ Обновление Framework по доению коровы\n☑ Проверка OSEMENIT.Bimbim\n☑ Согласование вязанки с генсухой\n☑ Проверка наличия гуся\n☑ Сертификация тараканов\n☑ Проверка Workrave на способность говорить «моргай»", Location = new Point(40, 85), Size = new Size(760, 190), Font = new Font("Tahoma", 11F) });
        p.Controls.Add(new Label { Text = "Нажмите «Далее», чтобы начать установку. После этого мастер будет некоторое время делать вид, что работает.", Location = new Point(40, 310), Size = new Size(700, 60), ForeColor = Color.Maroon });
        pageHost.Controls.Add(p);
    }

    private void BuildInstall()
    {
        var p = RetroSurface();
        p.Controls.Add(new Label { Text = "Файлы копируются. Жизненно важные решения принимаются.", Location = new Point(28, 24), AutoSize = true, Font = new Font("Tahoma", 13F, FontStyle.Bold) });
        p.Controls.Add(new Label { Text = "CutVPN.exe\nprank.state\ncheburetnet.cfg\ngus.dll\nvazyanka.sys\nOSEMENIT.Bimbim", Location = new Point(40, 85), Size = new Size(480, 190), Font = new Font("Consolas", 11F) });
        p.Controls.Add(new Label { Text = "Не выключайте мастер. Он почти понимает, что происходит.", Location = new Point(40, 300), AutoSize = true, ForeColor = Color.Navy });
        pageHost.Controls.Add(p);
    }

    private void BuildFinish()
    {
        var p = RetroSurface();
        p.Controls.Add(new Label { Text = "УСТАНОВКА ЗАВЕРШЕНА", Location = new Point(28, 25), AutoSize = true, Font = new Font("Tahoma", 18F, FontStyle.Bold), ForeColor = Color.DarkGreen });
        p.Controls.Add(new Label { Text = "CutVPN установлен в:\n\n" + InstallPaths.Root + "\n\nГусь: подготовлен\nТараканы: подготовлены\nWorkrave: подготовлен\nПранк-визуалы: " + (visuals.Checked ? "ВКЛ" : "ВЫКЛ") + "\nАвтозапуск CutVPN: " + (startup.Checked ? "ВКЛ" : "ВЫКЛ"), Location = new Point(35, 90), Size = new Size(650, 220), Font = new Font("Tahoma", 11F) });
        p.Controls.Add(new Label { Text = "Теперь можно нажать «Готово». Гусь рекомендует не сопротивляться.", Location = new Point(35, 335), AutoSize = true, ForeColor = Color.Navy });
        pageHost.Controls.Add(p);
    }

    private void StartInstallation()
    {
        installing = true;
        fakeProgress = 0;
        installProgress.Value = 0;
        statusLine.Text = statuses[0];
        installTimer.Start();
        ShowPage();
    }

    private void InstallTick()
    {
        fakeProgress = Math.Min(100, fakeProgress + (fakeProgress < 70 ? 1 : 2));
        installProgress.Value = fakeProgress;
        statusLine.Text = statuses[(fakeProgress / 5) % statuses.Length];
        if (fakeProgress >= 100)
        {
            installTimer.Stop();
            PerformInstall();
            installing = false;
            pageIndex = 7;
            ShowPage();
        }
    }

    private void PerformInstall()
    {
        var config = new SetupConfig
        {
            Goose = goose.Checked,
            Cockroach = cockroach.Checked,
            Workrave = workrave.Checked,
            Visuals = visuals.Checked,
            Startup = startup.Checked,
            Nationality = nationality.Text,
            Children = (int)children.Value,
            Empire = empire.Text
        };

        Directory.CreateDirectory(InstallPaths.Root);
        File.WriteAllText(InstallPaths.Config, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(InstallPaths.Readme, "CutVPN Prank Edition\r\nКомпоненты: Goose, Cockroach, Workrave, Visuals\r\n");

        var current = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(current)) File.Copy(current, InstallPaths.Exe, true);

        InstallPaths.SetStartup(config.Startup);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey && m.WParam.ToInt32() == HotkeyId)
        {
            Close();
            return;
        }
        base.WndProc(ref m);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        installTimer.Stop();
        UnregisterHotKey(Handle, HotkeyId);
        base.OnFormClosed(e);
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

internal static class InstallPaths
{
    public static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CutVPN");
    public static string Exe => Path.Combine(Root, "CutVPN.exe");
    public static string Config => Path.Combine(Root, "config.json");
    public static string Readme => Path.Combine(Root, "installed-components.txt");

    public static void SetStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
        if (key == null) return;
        if (enabled) key.SetValue("CutVPN", Exe);
        else key.DeleteValue("CutVPN", false);
    }
}

internal sealed class RetroPoster : Panel
{
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var bg = new LinearGradientBrush(ClientRectangle, Color.Teal, Color.SteelBlue, 25f);
        e.Graphics.FillRectangle(bg, ClientRectangle);
        using var white = new SolidBrush(Color.White);
        using var yellow = new SolidBrush(Color.Yellow);
        e.Graphics.DrawString("WINDOWS", new Font("Tahoma", 24F, FontStyle.Bold), white, 18, 18);
        e.Graphics.DrawString("ХУЯК XP/VISTA", new Font("Tahoma", 14F, FontStyle.Bold), yellow, 18, 52);
        e.Graphics.FillRectangle(Brushes.White, 18, 90, Width - 36, 105);
        e.Graphics.DrawRectangle(Pens.Navy, 18, 90, Width - 36, 105);
        e.Graphics.DrawString("ПЕРСОНАЛЬНЫЕ\nПРЕДЛОЖЕНИЯ", new Font("Tahoma", 11F, FontStyle.Bold), Brushes.Navy, 30, 105);
        e.Graphics.DrawString("Гусь подключён.\nВязанка загружена.\nОсеменение: 97%.", new Font("Tahoma", 8.5F), Brushes.Black, 30, 145);
    }
}
