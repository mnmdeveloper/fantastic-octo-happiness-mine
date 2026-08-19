using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CutVPN.Setup;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        Application.Run(args.Contains("--installed", StringComparer.OrdinalIgnoreCase)
            ? new CutVPN.AppContext()
            : new SetupWizard());
    }
}

internal sealed class CutVPNConfig
{
    public bool Goose { get; set; } = true;
    public bool Cockroach { get; set; } = true;
    public bool Workrave { get; set; } = true;
    public bool Visuals { get; set; } = true;
    public bool Startup { get; set; } = true;
    public string Nationality { get; set; } = "Чебурек";
    public int Children { get; set; } = 1;
    public string Empire { get; set; } = "Империя Чебурнета";
    public int EmpireScale { get; set; } = 40;
}

internal static class InstallState
{
    public static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CutVPN");
    public static string Exe => Path.Combine(Root, "CutVPN.exe");
    public static string Config => Path.Combine(Root, "config.json");
    public static string Startup => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "CutVPN.cmd");

    public static CutVPNConfig Load()
    {
        try
        {
            if (File.Exists(Config))
                return JsonSerializer.Deserialize<CutVPNConfig>(File.ReadAllText(Config)) ?? new CutVPNConfig();
        }
        catch { }
        return new CutVPNConfig();
    }

    public static void Save(CutVPNConfig value)
    {
        Directory.CreateDirectory(Root);
        File.WriteAllText(Config, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static void SetStartup(bool enabled)
    {
        if (!enabled)
        {
            if (File.Exists(Startup)) File.Delete(Startup);
            return;
        }
        var text = $"@echo off\r\nstart \"\" \"{Exe}\" --installed\r\n";
        File.WriteAllText(Startup, text);
    }
}

internal sealed class SetupWizard : Form
{
    private readonly Panel host;
    private readonly Label heading;
    private readonly Label description;
    private readonly Label news;
    private readonly ProgressBar progress;
    private readonly Label progressText;
    private readonly Button back;
    private readonly Button next;
    private readonly Button cancel;
    private readonly Timer installTimer;

    private readonly CheckBox goose = new() { Text = "Desktop Goose — важнейший сетевой гусь", AutoSize = true, Checked = true };
    private readonly CheckBox cockroach = new() { Text = "Cockroach on Desktop — анти-клоповый режим", AutoSize = true, Checked = true };
    private readonly CheckBox workrave = new() { Text = "Workrave — улучшение зрения не вставая из-за ПК", AutoSize = true, Checked = true };
    private readonly CheckBox visuals = new() { Text = "CutVPN Prank Visuals — рекомендуется", AutoSize = true, Checked = true };
    private readonly CheckBox startup = new() { Text = "Запускать CutVPN при входе в Windows", AutoSize = true, Checked = true };
    private readonly TextBox nationality = new() { Text = "Чебурек", Width = 280 };
    private readonly NumericUpDown children = new() { Minimum = 0, Maximum = 99, Value = 1, Width = 90 };
    private readonly ComboBox empire = new() { Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TrackBar empireScale = new() { Minimum = 0, Maximum = 100, Value = 40, TickFrequency = 10, Width = 300 };

    private int page;
    private int fakeProgress;
    private bool hotkeys;
    private readonly string[] steps =
    {
        "Добро пожаловать",
        "Параметры Интернета для локальной сети",
        "Персональные предложения",
        "Свойства: сясь кран",
        "Выбор обязательных компонентов",
        "Подтверждение системной безопасности",
        "Установка CutVPN",
        "Готово"
    };

    public SetupWizard()
    {
        Text = "Мастер шиттинга Чебурнета — CutVPN Setup";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        BackColor = Color.FromArgb(192, 192, 192);
        Font = new Font("Tahoma", 10F);
        KeyPreview = true;
        DoubleBuffered = true;

        empire.Items.AddRange(new object[] { "Империя Чебурнета", "Гусландия", "Вязаночная область", "Территория генсухи" });
        empire.SelectedIndex = 0;

        var title = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.FromArgb(0, 0, 128) };
        Controls.Add(title);
        title.Controls.Add(new Label { Text = "Мастер шиттинга Чебурнета", ForeColor = Color.White, Font = new Font("Tahoma", 16F, FontStyle.Bold), Location = new Point(20, 14), AutoSize = true });

        var exit = new Button { Text = "X", Font = new Font("Tahoma", 14F, FontStyle.Bold), Size = new Size(38, 38) };
        exit.Click += (_, _) => Close();
        title.Controls.Add(exit);
        title.Resize += (_, _) => exit.Location = new Point(title.Width - 50, 9);

        var left = new Panel { Location = new Point(18, 74), Width = 250, Height = Screen.PrimaryScreen!.Bounds.Height - 160, BackColor = Color.FromArgb(225, 225, 225), BorderStyle = BorderStyle.FixedSingle };
        Controls.Add(left);
        left.Controls.Add(new RetroPoster { Dock = DockStyle.Top, Height = 215 });
        left.Controls.Add(new Label { Text = "CUTVPN 98.XP-ЯК\r\n\r\nПроверено генсухой\r\nПодтверждено гусём\r\nОсновано на вязанке\r\n\r\nСостояние: работает, наверное.", Location = new Point(14, 230), Size = new Size(220, 170), Font = new Font("Tahoma", 9F) });

        host = new Panel { Location = new Point(286, 74), Size = new Size(Screen.PrimaryScreen.Bounds.Width - 305, Screen.PrimaryScreen.Bounds.Height - 160), BackColor = Color.White, BorderStyle = BorderStyle.Fixed3D };
        Controls.Add(host);

        heading = new Label { Location = new Point(28, 24), AutoSize = true, Font = new Font("Tahoma", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 0, 128) };
        description = new Label { Location = new Point(28, 68), Size = new Size(host.Width - 56, 240), Font = new Font("Tahoma", 11F) };
        news = new Label { Location = new Point(28, 330), Size = new Size(host.Width - 56, 44), ForeColor = Color.Navy, Font = new Font("Tahoma", 9F, FontStyle.Italic) };
        progress = new ProgressBar { Location = new Point(28, 395), Size = new Size(host.Width - 56, 28), Visible = false };
        progressText = new Label { Location = new Point(28, 430), AutoSize = true };
        host.Controls.AddRange(new Control[] { heading, description, news, progress, progressText });

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 74, BackColor = Color.FromArgb(212, 212, 212) };
        Controls.Add(footer);
        back = new Button { Text = "< Назад", Size = new Size(110, 34) };
        next = new Button { Text = "Далее >", Size = new Size(110, 34) };
        cancel = new Button { Text = "Отмена", Size = new Size(110, 34) };
        footer.Controls.AddRange(new Control[] { back, next, cancel });
        footer.Resize += (_, _) =>
        {
            cancel.Location = new Point(footer.Width - 130, 20);
            next.Location = new Point(footer.Width - 250, 20);
            back.Location = new Point(footer.Width - 370, 20);
        };
        back.Click += (_, _) => Navigate(-1);
        next.Click += (_, _) => Navigate(1);
        cancel.Click += (_, _) => Close();

        installTimer = new Timer { Interval = 110 };
        installTimer.Tick += (_, _) => InstallTick();
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        HandleCreated += (_, _) => RegisterHotkeys();
        FormClosed += (_, _) => UnregisterHotkeys();
        Resize += (_, _) => LayoutPanels();

        ShowPage();
    }

    private void LayoutPanels()
    {
        if (host == null) return;
        host.Height = ClientSize.Height - 160;
        host.Width = ClientSize.Width - 305;
    }

    private void RegisterHotkeys()
    {
        hotkeys = Native.RegisterHotKey(Handle, 1001, Native.MOD_WIN, (int)Keys.U);
        Native.RegisterHotKey(Handle, 1002, Native.MOD_CONTROL | Native.MOD_SHIFT, (int)Keys.G);
    }

    private void UnregisterHotkeys()
    {
        if (!hotkeys) return;
        Native.UnregisterHotKey(Handle, 1001);
        Native.UnregisterHotKey(Handle, 1002);
        hotkeys = false;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Native.WM_HOTKEY)
        {
            var id = m.WParam.ToInt32();
            if (id is 1001 or 1002) { Close(); return; }
        }
        base.WndProc(ref m);
    }

    private void Navigate(int delta)
    {
        if (page == 6 && delta > 0 && fakeProgress < 100) return;
        if (page == 7 && delta > 0)
        {
            LaunchInstalled();
            return;
        }

        page = Math.Clamp(page + delta, 0, steps.Length - 1);
        if (page == 6)
        {
            fakeProgress = 0;
            progress.Value = 0;
            installTimer.Start();
        }
        else
        {
            installTimer.Stop();
        }
        ShowPage();
    }

    private void ShowPage()
    {
        heading.Text = steps[page];
        description.Text = string.Empty;
        news.Text = string.Empty;
        progress.Visible = page == 6;
        progressText.Visible = page == 6;
        back.Enabled = page > 0 && page != 6 && page != 7;
        cancel.Enabled = page < 7;
        next.Enabled = page != 6;
        next.Text = page == 7 ? "Запустить CutVPN" : "Далее >";
        host.Controls.Clear();
        host.Controls.AddRange(new Control[] { heading, description, news, progress, progressText });

        switch (page)
        {
            case 0: Welcome(); break;
            case 1: Internet(); break;
            case 2: Personal(); break;
            case 3: Crane(); break;
            case 4: Components(); break;
            case 5: Security(); break;
            case 6: Install(); break;
            case 7: Finish(); break;
        }
    }

    private void Welcome()
    {
        description.Text = "CutVPN подготовит вашу систему к безопасному\r\n\r\nОБЫЧНОМУ ИНТЕРНЕТУ ЧЕБУРНЕТА.\r\n\r\nВ установщике есть гусь, тараканы, Workrave, генсуха, вязанка и осеменение по необходимости.\r\n\r\nЭто стилизация под старые установщики Windows 98/XP: серые панели, синие заголовки, странные настройки и абсолютно серьёзные сообщения.";
        host.Controls.Add(new RetroMonitor { Location = new Point(host.Width - 290, 100), Size = new Size(230, 170) });
        news.Text = "СРОЧНАЯ НОВОСТЬ: вязанка снова была замечена рядом с генсухой. Гусь всё отрицает.";
    }

    private void Internet()
    {
        description.Text = "Выберите способ настройки параметров прокси-сервера. Если вы не знаете, что выбрать, выберите автоматическое определение.\r\n\r\nИспользование этого экрана не меняет реальный прокси Windows — CutVPN просто бережно изображает старый мастер.";
        var box = new GroupBox { Text = "Автоматическая настройка", Location = new Point(28, 185), Size = new Size(host.Width - 56, 170) };
        box.Controls.Add(new CheckBox { Text = "Автоматическое определение прокси-сервера (рекомендуется)", Location = new Point(18, 28), AutoSize = true, Checked = true });
        box.Controls.Add(new CheckBox { Text = "Использовать сценарий автоматической настройки", Location = new Point(18, 62), AutoSize = true });
        box.Controls.Add(new Label { Text = "Адрес:", Location = new Point(18, 103), AutoSize = true });
        box.Controls.Add(new TextBox { Text = "http://proxy.cheburetnet.local/auto.pac", Location = new Point(82, 99), Width = Math.Min(520, host.Width - 180) });
        host.Controls.Add(box);
        host.Controls.Add(new CheckBox { Text = "Ручная настройка прокси-сервера", Location = new Point(30, 375), AutoSize = true });
    }

    private void Personal()
    {
        description.Text = "Георгий просит вас предоставить все персональные данные для их обработки и хранения. Вся информация будет храниться до 10380 дней. После этого система всё забудет и предложит пройти процедуру заново.";
        AddField("Кто вы по национальности?", nationality, 165);
        AddField("Количество детей в семье", children, 215);
        var gooseName = new TextBox { Text = "Гусь", Width = 280 };
        AddField("Как зовут вашего гуся?", gooseName, 265);
        var agree = new CheckBox { Text = "Я готов к осеменению по необходимости", AutoSize = true, Checked = true };
        agree.Location = new Point(30, 315);
        host.Controls.Add(agree);
        host.Controls.Add(new Label { Text = "Кнопка «Далее» автоматически означает согласие со всем вышеизложенным.", Location = new Point(30, 360), AutoSize = true, ForeColor = Color.Maroon });
    }

    private void AddField(string label, Control input, int top)
    {
        host.Controls.Add(new Label { Text = label, Location = new Point(30, top + 4), AutoSize = true });
        input.Location = new Point(290, top);
        host.Controls.Add(input);
    }

    private void Crane()
    {
        description.Text = "Служебные параметры вашего sаmsung кран. Некоторые поля специально не имеют никакого отношения к VPN. Если параметр непонятен — значит, он работает.";
        host.Controls.Add(new Label { Text = "Ваш гендер:", Location = new Point(30, 170), AutoSize = true });
        var gender = new ComboBox { Location = new Point(150, 166), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
        gender.Items.AddRange(new object[] { "АНАНАС SPIR PRO(много)", "Гусь", "Вязанка", "Кран" });
        gender.SelectedIndex = 0;
        host.Controls.Add(gender);
        host.Controls.Add(new Label { Text = "Область империи:", Location = new Point(30, 220), AutoSize = true });
        empire.Location = new Point(150, 216);
        host.Controls.Add(empire);
        host.Controls.Add(new Label { Text = "мало", Location = new Point(30, 285), AutoSize = true });
        empireScale.Location = new Point(75, 275);
        host.Controls.Add(empireScale);
        host.Controls.Add(new Label { Text = "до хуя", Location = new Point(385, 285), AutoSize = true });
        host.Controls.Add(new Label { Text = "Проверка крана завершена: кран существует.", Location = new Point(30, 345), AutoSize = true, ForeColor = Color.DarkGreen });
    }

    private void Components()
    {
        description.Text = "Выберите компоненты. CutVPN не скачивает сторонние программы скрытно: если рядом с установщиком есть папка installer\\payload, её файлы будут скопированы в установленную папку как пользовательские payload-компоненты.";
        var checks = new[] { goose, cockroach, workrave, visuals, startup };
        for (var i = 0; i < checks.Length; i++)
        {
            checks[i].Location = new Point(30, 165 + i * 45);
            host.Controls.Add(checks[i]);
        }
        host.Controls.Add(new Label { Text = "Реклама: улучшение зрения, уничтожение клопов и продажа гуся — три услуги за одну кнопку.", Location = new Point(30, 390), AutoSize = true, ForeColor = Color.Navy });
    }

    private void Security()
    {
        description.Text = "СИСТЕМНАЯ БЕЗОПАСНОСТЬ\r\n\r\n☑ Framework по доению коровы\r\n☑ Проверка OSEMENIT.Bimbim\r\n☑ Согласование вязанки с генсухой\r\n☑ Проверка наличия гуся\r\n\r\nНикаких реальных системных параметров этот экран не меняет.";
        host.Controls.Add(new RetroProgressBox { Location = new Point(30, 330), Size = new Size(host.Width - 60, 55) });
    }

    private void Install()
    {
        description.Text = "Установка CutVPN\r\n\r\nПодождите, пока мастер выполняет чрезвычайно важные действия.\r\n\r\nГусь: в процессе\r\nТараканы: почти готовы\r\nWorkrave: моргает\r\nЧебурнет: нестабилен";
        progress.Value = fakeProgress;
        progressText.Text = "Упорядочиваем вязанку...";
        back.Enabled = false;
        next.Enabled = false;
        cancel.Enabled = false;
    }

    private void InstallTick()
    {
        fakeProgress = Math.Min(100, fakeProgress + Random.Shared.Next(1, 4));
        progress.Value = fakeProgress;
        progressText.Text = new[]
        {
            "Упорядочиваем вязанку...",
            "Согласовываем с генсухой...",
            "Ищем OSEMENIT.Bimbim...",
            "Проверяем, где гусь...",
            "Ускоряем Чебурнет...",
            "Спрашиваем клопов, всё ли им нравится..."
        }[Random.Shared.Next(6)];

        if (Random.Shared.NextDouble() < 0.02)
        {
            var errors = new[]
            {
                "Для доения коровы не хватает модуля: OSEMENIT.Bimbim",
                "GENSUHA.dll отказалась согласовывать вязанку.",
                "Гусь обнаружил нарушение протокола Чебурнета.",
                "VYAZANKA.sys требует дополнительного осеменения.",
                "Ошибка 0x8004GUS: гусь занят важными делами."
            };
            MessageBox.Show(errors[Random.Shared.Next(errors.Length)], "CutVPN — тупая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        if (fakeProgress >= 100)
        {
            installTimer.Stop();
            PerformInstall();
            page = 7;
            ShowPage();
        }
    }

    private void PerformInstall()
    {
        Directory.CreateDirectory(InstallState.Root);
        var config = new CutVPNConfig
        {
            Goose = goose.Checked,
            Cockroach = cockroach.Checked,
            Workrave = workrave.Checked,
            Visuals = visuals.Checked,
            Startup = startup.Checked,
            Nationality = nationality.Text,
            Children = (int)children.Value,
            Empire = empire.Text,
            EmpireScale = empireScale.Value
        };
        InstallState.Save(config);

        var current = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(current))
        {
            var target = InstallState.Exe;
            if (!string.Equals(Path.GetFullPath(current), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
                File.Copy(current, target, true);
        }

        InstallState.SetStartup(config.Startup);

        var payload = Path.Combine(AppContext.BaseDirectory, "payload");
        var installedPayload = Path.Combine(InstallState.Root, "payload");
        if (Directory.Exists(payload))
        {
            Directory.CreateDirectory(installedPayload);
            foreach (var file in Directory.GetFiles(payload))
                File.Copy(file, Path.Combine(installedPayload, Path.GetFileName(file)), true);
        }
    }

    private void Finish()
    {
        description.Text = "Установка завершена.\r\n\r\nCutVPN установлен в:\r\n" + InstallState.Root + "\r\n\r\nКонфигурация сохранена. Старт с Windows: " + (startup.Checked ? "ВКЛ" : "ВЫКЛ") + ".\r\n\r\nГусь доволен. Вязанка сохранена. Чебурнет готов.";
        news.Text = "Нажмите «Запустить CutVPN», чтобы открыть установленную копию.";
        next.Enabled = true;
        next.Text = "Запустить CutVPN";
        cancel.Enabled = false;
        back.Enabled = false;
    }

    private void LaunchInstalled()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(InstallState.Exe, "--installed") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Не удалось запустить CutVPN:\r\n" + ex.Message, "CutVPN Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        Close();
    }
}

namespace CutVPN
{
    internal sealed class AppContext : ApplicationContext
    {
        private readonly NotifyIcon tray;
        private readonly CutVPNConfig config;
        private MainForm? main;
        private bool hotkeys;

        public AppContext()
        {
            config = LoadConfig();
            tray = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "CutVPN",
                Visible = true,
                ContextMenuStrip = BuildMenu()
            };
            tray.DoubleClick += (_, _) => ShowMain();
            if (config.Visuals) ShowPrank(); else ShowMain();
        }

        private static CutVPNConfig LoadConfig()
        {
            try
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CutVPN", "config.json");
                return File.Exists(path) ? JsonSerializer.Deserialize<CutVPNConfig>(File.ReadAllText(path)) ?? new CutVPNConfig() : new CutVPNConfig();
            }
            catch { return new CutVPNConfig(); }
        }

        private ContextMenuStrip BuildMenu()
        {
            var m = new ContextMenuStrip();
            m.Items.Add("Открыть CutVPN", null, (_, _) => ShowMain());
            m.Items.Add("🎭 Включить визуалы", null, (_, _) => Toggle(true));
            m.Items.Add("🛑 Отключить визуалы", null, (_, _) => Toggle(false));
            m.Items.Add("💥 Тупая ошибка", null, (_, _) => RandomError());
            m.Items.Add("📰 Обновление системы безопасности", null, (_, _) => SecurityUpdate());
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add("Выйти из CutVPN", null, (_, _) => ExitThread());
            return m;
        }

        private void ShowMain()
        {
            if (main == null || main.IsDisposed) main = new MainForm(config);
            main.Show();
            main.WindowState = FormWindowState.Normal;
            main.Activate();
        }

        private void ShowPrank()
        {
            using var prank = new PrankWindow(config);
            prank.ShowDialog();
            if (!prank.EmergencyStopped) ShowMain();
        }

        private void Toggle(bool value)
        {
            config.Visuals = value;
            InstallState.Save(config);
            if (value) ShowPrank(); else tray.ShowBalloonTip(900, "CutVPN", "Визуалы отключены.", ToolTipIcon.Info);
        }

        private void RandomError()
        {
            var errors = new[]
            {
                "Для доения коровы не хватает модуля: OSEMENIT.Bimbim",
                "GENSUHA.dll отказалась согласовывать вязанку.",
                "Гусь обнаружил нарушение протокола Чебурнета.",
                "VYAZANKA.sys требует дополнительного осеменения.",
                "Ошибка 0x8004GUS: гусь занят важными делами."
            };
            MessageBox.Show(errors[Random.Shared.Next(errors.Length)], "CutVPN — тупая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void SecurityUpdate()
        {
            using var update = new PrankWindow(config, "Обновление системы безопасности");
            update.ShowDialog();
        }

        public override void ExitThread()
        {
            main?.Close();
            tray.Visible = false;
            tray.Dispose();
            base.ExitThread();
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly CutVPNConfig config;
        public MainForm(CutVPNConfig config)
        {
            this.config = config;
            Text = "CutVPN — Connection Manager";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(820, 520);
            BackColor = Color.FromArgb(236, 233, 216);
            Font = new Font("Tahoma", 9F);

            var top = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(0, 0, 128) };
            Controls.Add(top);
            top.Controls.Add(new Label { Text = "CutVPN", ForeColor = Color.White, Font = new Font("Tahoma", 16F, FontStyle.Bold), Location = new Point(14, 10), AutoSize = true });

            var body = new Panel { Location = new Point(16, 64), Size = new Size(788, 385), BorderStyle = BorderStyle.Fixed3D, BackColor = Color.White };
            Controls.Add(body);
            body.Controls.Add(new Label { Text = "Virtual Private Network Connection", Location = new Point(24, 20), AutoSize = true, Font = new Font("Tahoma", 15F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 51, 153) });
            body.Controls.Add(new Label { Text = "Очень серьёзный сетевой продукт с абсолютно несерьёзным содержимым.", Location = new Point(24, 52), AutoSize = true, ForeColor = Color.DimGray });
            body.Controls.Add(new Label { Text = "VPN Server:", Location = new Point(24, 98), AutoSize = true });
            var server = new ComboBox { Location = new Point(105, 94), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            server.Items.AddRange(new object[] { "CutVPN Europe", "Cheburetnet Home", "GENSUHA Turbo", "Automatic" });
            server.SelectedIndex = 0;
            body.Controls.Add(server);
            var connect = new Button { Text = "Connect", Location = new Point(430, 91), Size = new Size(120, 30) };
            body.Controls.Add(connect);
            var status = new Label { Text = "Status: Disconnected", Location = new Point(24, 142), AutoSize = true, Font = new Font("Tahoma", 10F, FontStyle.Bold), ForeColor = Color.Maroon };
            body.Controls.Add(status);
            body.Controls.Add(new Label { Text = "Connection information\r\n\r\nServer       CutVPN Europe\r\nProtocol     Automatic\r\nIP address   Demo / not connected", Location = new Point(24, 180), AutoSize = true });
            var prank = new CheckBox { Text = "Запускать визуалы при входе в Windows", Location = new Point(24, 305), AutoSize = true, Checked = config.Visuals };
            body.Controls.Add(prank);
            prank.CheckedChanged += (_, _) => { config.Visuals = prank.Checked; InstallState.Save(config); };
            body.Controls.Add(new Label { Text = "Правый клик по значку CutVPN в трее открывает пульт. Ctrl+Shift+G — стоп-кран.", Location = new Point(24, 340), AutoSize = true, ForeColor = Color.DimGray });
            connect.Click += (_, _) =>
            {
                var connected = status.Text == "Status: Disconnected";
                status.Text = connected ? "Status: Connected" : "Status: Disconnected";
                status.ForeColor = connected ? Color.DarkGreen : Color.Maroon;
                connect.Text = connected ? "Disconnect" : "Connect";
            };
        }
    }

    internal sealed class PrankWindow : Form
    {
        private readonly CutVPNConfig config;
        private readonly Label title;
        private readonly Label body;
        private readonly ProgressBar progress;
        private readonly Timer timer;
        private int fakeProgress;
        public bool EmergencyStopped { get; private set; }

        public PrankWindow(CutVPNConfig config, string windowTitle = "CutVPN — Мастер шиттинга Чебурнета")
        {
            this.config = config;
            Text = windowTitle;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            BackColor = Color.FromArgb(192, 192, 192);
            KeyPreview = true;
            DoubleBuffered = true;

            var top = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.FromArgb(0, 0, 128) };
            Controls.Add(top);
            title = new Label { Text = windowTitle, ForeColor = Color.White, Font = new Font("Tahoma", 16F, FontStyle.Bold), Location = new Point(20, 14), AutoSize = true };
            top.Controls.Add(title);
            var exit = new Button { Text = "X", Font = new Font("Tahoma", 14F, FontStyle.Bold), Size = new Size(38, 38) };
            exit.Click += (_, _) => Close();
            top.Controls.Add(exit);
            top.Resize += (_, _) => exit.Location = new Point(top.Width - 50, 9);

            var main = new Panel { Location = new Point(18, 74), Size = new Size(Screen.PrimaryScreen!.Bounds.Width - 36, Screen.PrimaryScreen.Bounds.Height - 112), BackColor = Color.White, BorderStyle = BorderStyle.Fixed3D };
            Controls.Add(main);
            main.Controls.Add(new RetroPoster { Location = new Point(22, 22), Size = new Size(220, 195) });
            main.Controls.Add(new Label { Text = "Персональные предложения", Location = new Point(275, 28), AutoSize = true, Font = new Font("Tahoma", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 0, 128) });
            body = new Label { Text = "Хотите улучшить зрение, не вставая из-за ПК?\r\n\r\nCutVPN совершенно случайно нашёл для вас Workrave.\r\nПоставим? Конечно поставим. Мы уже почти всё решили.", Location = new Point(275, 84), Size = new Size(main.Width - 310, 220), Font = new Font("Tahoma", 12F) };
            main.Controls.Add(body);
            main.Controls.Add(new Label { Text = "СРОЧНАЯ НОВОСТЬ: вязанка снова была замечена рядом с генсухой. Гусь это отрицает.", Location = new Point(275, 315), Size = new Size(main.Width - 310, 45), ForeColor = Color.Navy, Font = new Font("Tahoma", 9F, FontStyle.Italic) });
            progress = new ProgressBar { Location = new Point(275, 390), Size = new Size(main.Width - 310, 26), Maximum = 100 };
            main.Controls.Add(progress);
            main.Controls.Add(new Label { Text = "Esc / Win+U — выйти. Ctrl+Shift+G — аварийный стоп.", Location = new Point(275, 430), AutoSize = true, ForeColor = Color.DimGray });

            timer = new Timer { Interval = 170 };
            timer.Tick += (_, _) => Animate();
            timer.Start();
            HandleCreated += (_, _) => { Native.RegisterHotKey(Handle, 3001, Native.MOD_WIN, (int)Keys.U); Native.RegisterHotKey(Handle, 3002, Native.MOD_CONTROL | Native.MOD_SHIFT, (int)Keys.G); };
            FormClosed += (_, _) => { timer.Stop(); Native.UnregisterHotKey(Handle, 3001); Native.UnregisterHotKey(Handle, 3002); };
            KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_HOTKEY)
            {
                var id = m.WParam.ToInt32();
                if (id == 3001) { Close(); return; }
                if (id == 3002) { EmergencyStopped = true; config.Visuals = false; InstallState.Save(config); Close(); return; }
            }
            base.WndProc(ref m);
        }

        private void Animate()
        {
            fakeProgress = Math.Min(100, fakeProgress + Random.Shared.Next(1, 4));
            progress.Value = fakeProgress;
            var statuses = new[] { "Упорядочиваем вязанку...", "Согласовываем с генсухой...", "Ищем OSEMENIT.Bimbim...", "Проверяем, где гусь...", "Ускоряем Чебурнет...", "Спрашиваем клопов, всё ли им нравится..." };
            body.Text = "Хотите улучшить зрение, не вставая из-за ПК?\r\n\r\n" + statuses[Random.Shared.Next(statuses.Length)] + "\r\n\r\nПожалуйста, подождите. Это исключительно серьёзный этап.";
            if (fakeProgress >= 100)
            {
                timer.Stop();
                body.Text = "Готово.\r\n\r\nГусь доволен. Вязанка сохранена.\r\n\r\nТеперь можно закрыть это окно.";
            }
        }
    }

    internal sealed class RetroPoster : Panel
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var bg = new LinearGradientBrush(ClientRectangle, Color.Navy, Color.Teal, 40f);
            e.Graphics.FillRectangle(bg, ClientRectangle);
            e.Graphics.DrawString("WINDOWS", new Font("Tahoma", 20F, FontStyle.Bold), Brushes.White, 16, 15);
            e.Graphics.DrawString("ХУЯК-98 XP/VISTA", new Font("Tahoma", 12F, FontStyle.Bold), Brushes.Yellow, 16, 48);
            e.Graphics.FillRectangle(Brushes.White, 16, 82, Width - 32, 100);
            e.Graphics.DrawRectangle(Pens.Navy, 16, 82, Width - 32, 100);
            e.Graphics.DrawString("ВНИМАНИЕ!", new Font("Tahoma", 12F, FontStyle.Bold), Brushes.DarkRed, 28, 97);
            e.Graphics.DrawString("Гусь подключён.\r\nВязанка загружена.\r\nОсеменение в процессе.", new Font("Tahoma", 8F), Brushes.Black, 28, 126);
            e.Graphics.FillEllipse(Brushes.White, Width - 75, 104, 38, 38);
            e.Graphics.FillEllipse(Brushes.DarkGray, Width - 68, 113, 8, 8);
            e.Graphics.FillEllipse(Brushes.DarkGray, Width - 52, 113, 8, 8);
        }
    }

    internal static class Native
    {
        public const int WM_HOTKEY = 0x0312;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
