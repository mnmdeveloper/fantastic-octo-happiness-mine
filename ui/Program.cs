using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CutVPN;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.Run(new CutVpnApp());
    }
}

internal sealed class CutVpnApp : ApplicationContext
{
    private readonly NotifyIcon tray;
    private MainForm? main;
    private bool prankMode;

    internal CutVpnApp()
    {
        prankMode = LoadPrankState();
        tray = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "CutVPN — Cheburetnet",
            Visible = true,
            ContextMenuStrip = BuildTrayMenu()
        };
        tray.DoubleClick += (_, _) => ShowMain();
        if (prankMode) ShowWizard(); else ShowMain();
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var m = new ContextMenuStrip();
        m.Items.Add("Открыть CutVPN", null, (_, _) => ShowMain());
        m.Items.Add("Включить пранк", null, (_, _) => SetPrank(true));
        m.Items.Add("Отключить визуалы", null, (_, _) => SetPrank(false));
        m.Items.Add("Обновление системы безопасности", null, (_, _) => ShowSecurityUpdate());
        m.Items.Add("Случайная хуйня", null, (_, _) => ShowRandomError());
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add("Стоп-кран Ctrl+Shift+G", null, (_, _) => EmergencyStop());
        m.Items.Add("Выйти из CutVPN", null, (_, _) => ExitApplication());
        return m;
    }

    internal void SetPrank(bool value)
    {
        prankMode = value;
        SavePrankState(value);
        if (value) ShowWizard();
        else tray.ShowBalloonTip(900, "CutVPN", "Визуалы выключены.", ToolTipIcon.Info);
    }

    internal void ShowMain()
    {
        if (main is null || main.IsDisposed) main = new MainForm(this);
        main.Show();
        main.WindowState = FormWindowState.Normal;
        main.Activate();
    }

    internal void ShowWizard()
    {
        using var wizard = new InstallWizard(this);
        wizard.ShowDialog();
        if (prankMode) ShowMain();
    }

    private void ShowSecurityUpdate()
    {
        using var wizard = new InstallWizard(this, "Обновление системы безопасности");
        wizard.ShowDialog();
    }

    private void ShowRandomError()
    {
        var text = PrankContent.RandomErrors[Random.Shared.Next(PrankContent.RandomErrors.Length)];
        MessageBox.Show(text, "CutVPN — Диагностика Чебурнета", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ExitApplication()
    {
        main?.Close();
        tray.Visible = false;
        tray.Dispose();
        Application.ExitThread();
    }

    internal void EmergencyStop()
    {
        prankMode = false;
        SavePrankState(false);
        tray.ShowBalloonTip(1000, "CutVPN", "Стоп-кран сработал. Визуалы отключены.", ToolTipIcon.Warning);
    }

    internal bool IsPrankMode => prankMode;

    private static string StatePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CutVPN", "prank.state");
    private static bool LoadPrankState() => File.Exists(StatePath) && File.ReadAllText(StatePath).Trim() == "on";

    private static void SavePrankState(bool enabled)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
        File.WriteAllText(StatePath, enabled ? "on" : "off");
    }
}

internal sealed class MainForm : Form
{
    private readonly CutVpnApp app;
    private readonly Label status;
    private readonly CheckBox prank;

    internal MainForm(CutVpnApp app)
    {
        this.app = app;
        Text = "CutVPN — Connection Manager";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(820, 520);
        MinimumSize = new Size(820, 520);
        BackColor = Color.FromArgb(236, 233, 216);
        Font = new Font("Tahoma", 9F);

        var titleBar = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.FromArgb(0, 0, 128) };
        Controls.Add(titleBar);
        titleBar.Controls.Add(new Label { Text = "CutVPN", ForeColor = Color.White, Font = new Font("Tahoma", 16F, FontStyle.Bold), AutoSize = true, Location = new Point(14, 10) });

        var body = new Panel { Location = new Point(15, 62), Size = new Size(790, 390), BorderStyle = BorderStyle.Fixed3D, BackColor = Color.White };
        Controls.Add(body);

        var logo = new Label { Text = "C", BackColor = Color.White, ForeColor = Color.FromArgb(0, 0, 128), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Tahoma", 28F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, Location = new Point(25, 24), Size = new Size(70, 70) };
        body.Controls.Add(logo);
        body.Controls.Add(new Label { Text = "Virtual Private Network Connection", Font = new Font("Tahoma", 15F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 51, 153), Location = new Point(115, 26), AutoSize = true });
        body.Controls.Add(new Label { Text = "Очень серьёзный сетевой продукт с абсолютно несерьёзным содержимым.", Location = new Point(118, 58), AutoSize = true, ForeColor = Color.DimGray });

        body.Controls.Add(new Label { Text = "VPN Server:", Location = new Point(30, 125), AutoSize = true });
        var server = new ComboBox { Location = new Point(110, 121), Size = new Size(300, 24), DropDownStyle = ComboBoxStyle.DropDownList };
        server.Items.AddRange(new object[] { "CutVPN Europe", "Cheburetnet Home", "GENSUHA Turbo", "Automatic" });
        server.SelectedIndex = 0;
        body.Controls.Add(server);

        var connect = new Button { Text = "Connect", Location = new Point(430, 118), Size = new Size(120, 30) };
        body.Controls.Add(connect);
        status = new Label { Text = "Status: Disconnected", Location = new Point(30, 171), AutoSize = true, Font = new Font("Tahoma", 10F, FontStyle.Bold), ForeColor = Color.Maroon };
        body.Controls.Add(status);
        body.Controls.Add(new Label { Text = "Connection information\n\nServer       CutVPN Europe\nProtocol     Automatic\nIP address   Demo / not connected", Location = new Point(30, 208), AutoSize = true });

        prank = new CheckBox { Text = "Запускать пранк CutVPN при входе в Windows", Location = new Point(30, 330), AutoSize = true, Checked = app.IsPrankMode };
        body.Controls.Add(prank);
        prank.CheckedChanged += (_, _) => { if (prank.Checked != app.IsPrankMode) { if (prank.Checked) app.ShowWizard(); else app.EmergencyStop(); } };

        connect.Click += (_, _) =>
        {
            bool connected = !status.Text.Contains("Connected", StringComparison.OrdinalIgnoreCase);
            status.Text = connected ? "Status: Connected" : "Status: Disconnected";
            status.ForeColor = connected ? Color.DarkGreen : Color.Maroon;
            connect.Text = connected ? "Disconnect" : "Connect";
        };
    }
}

internal sealed class InstallWizard : Form
{
    private readonly CutVpnApp app;
    private readonly ProgressBar progress;
    private readonly Label status;
    private readonly Label pageTitle;
    private readonly Label pageText;
    private readonly Label news;
    private readonly Button next;
    private readonly System.Windows.Forms.Timer timer;
    private int page;
    private int progressValue;

    private readonly string[] titles =
    {
        "Персональные предложения",
        "Спецпредложение: анти-клоп",
        "ЭКСКЛЮЗИВНЫЙ ГУСЬ",
        "Мастер шиттинга Чебурнета",
        "Системная безопасность",
        "Последние новости",
        "Проверка необходимых компонентов",
        "Финальная оптимизация"
    };

    private readonly string[] texts =
    {
        "Хотите улучшить зрение, не вставая из-за ПК?\n\nCutVPN совершенно случайно нашёл для вас Workrave.\n\nРекомендуется моргать. Желательно самостоятельно.",
        "В вашем доме подозрительно много насекомых.\n\nCockroach on Desktop уже подготовил решение.\n\nБорьба с клопами начнётся после нажатия «Далее». Если ничего не произойдёт — значит, клопы испугались.",
        "ПРОДАМ ГУСЯ.\n\nСостояние: бегает.\nКомплектация: клюв, лапы, гусь.\nГарантия: не предоставляется.\n\n[ КУПИТЬ ] — единственная разумная кнопка.",
        "Автоматическая настройка прокси-сервера.\n\nГенсуха согласует вязанку.\nОсеменение сетевого адаптера: 97%.\nГусь подключается к Чебурнету...",
        "Установка обновления Framework по доению коровы.\n\nПроверяются: GENSUHA.dll, VYAZANKA.sys, OSEMENIT.Bimbim.\n\nНе выключайте компьютер, пока инженер не найдёт нужную вязанку.",
        "СРОЧНАЯ НОВОСТЬ\n\nВязанка снова была замечена рядом с генсухой.\nГусь это отрицает.\n\nЭксперты продолжают наблюдение за Чебурнетом.",
        "Проверка компонентов\n\nWorkrave ............... готов\nCockroach on Desktop .... готов\nDesktop Goose ........... готов\nЧебурнет ................ почти\nОсеменение .............. непонятно",
        "Выполняется финальная оптимизация.\n\nСейчас мы скажем, что всё готово.\nПожалуйста, сделайте серьёзное лицо и дождитесь кнопки «Готово»."
    };

    internal InstallWizard(CutVpnApp app, string? customTitle = null)
    {
        this.app = app;
        Text = customTitle ?? "CutVPN — Мастер шиттинга Чебурнета";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.FromArgb(192, 192, 192);
        KeyPreview = true;

        var top = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.FromArgb(0, 0, 128) };
        Controls.Add(top);
        top.Controls.Add(new Label { Text = Text, ForeColor = Color.White, Font = new Font("Tahoma", 16F, FontStyle.Bold), Location = new Point(24, 14), AutoSize = true });
        var close = new Button { Text = "X", Font = new Font("Tahoma", 14F, FontStyle.Bold), Anchor = AnchorStyles.Top | AnchorStyles.Right, Size = new Size(38, 34), Location = new Point(Screen.PrimaryScreen!.Bounds.Width - 50, 12) };
        close.Click += (_, _) => Close();
        top.Controls.Add(close);

        var left = new Panel { Location = new Point(18, 78), Size = new Size(260, Screen.PrimaryScreen.Bounds.Height - 160), BackColor = Color.FromArgb(225, 225, 225), BorderStyle = BorderStyle.FixedSingle };
        Controls.Add(left);
        left.Controls.Add(new FakePoster { Dock = DockStyle.Top, Height = 230 });
        left.Controls.Add(new Label { Text = "CutVPN 98.ΞP-ЯК\n\nСертифицировано Генсухой.\nПроверено гусём.\nСогласовано вязанкой.\n\nСтатус: работает, наверное.", Location = new Point(14, 245), Size = new Size(230, 130), Font = new Font("Tahoma", 9F) });

        var content = new Panel { Location = new Point(300, 78), Size = new Size(Screen.PrimaryScreen.Bounds.Width - 325, Screen.PrimaryScreen.Bounds.Height - 160), BackColor = Color.White, BorderStyle = BorderStyle.Fixed3D, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };
        Controls.Add(content);
        pageTitle = new Label { Text = titles[0], Location = new Point(28, 26), AutoSize = true, Font = new Font("Tahoma", 20F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 0, 128) };
        content.Controls.Add(pageTitle);
        pageText = new Label { Text = texts[0], Location = new Point(28, 82), Size = new Size(content.Width - 56, 245), Font = new Font("Tahoma", 12F), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        content.Controls.Add(pageText);
        news = new Label { Text = "СРОЧНАЯ НОВОСТЬ: гусь сообщил, что вязанка снова ушла от ответственности.", Location = new Point(28, 338), Size = new Size(content.Width - 56, 46), ForeColor = Color.Navy, Font = new Font("Tahoma", 9F, FontStyle.Italic), Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };
        content.Controls.Add(news);

        progress = new ProgressBar { Location = new Point(28, 400), Size = new Size(content.Width - 56, 27), Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };
        content.Controls.Add(progress);
        status = new Label { Text = "Подготавливаем очень необходимые компоненты...", Location = new Point(28, 438), AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Bottom };
        content.Controls.Add(status);

        next = new Button { Text = "Далее >", Size = new Size(118, 36), Location = new Point(content.Width - 145, content.Height - 58), Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
        next.Click += (_, _) => NextPage();
        content.Controls.Add(next);
        var cancel = new Button { Text = "Отмена", Size = new Size(118, 36), Location = new Point(content.Width - 273, content.Height - 58), Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
        cancel.Click += (_, _) => Close();
        content.Controls.Add(cancel);

        var footer = new Label { Text = "CutVPN • Esc / Win+U — выйти из мастера • Ctrl+Shift+G — стоп-кран", Location = new Point(18, Screen.PrimaryScreen.Bounds.Height - 52), AutoSize = true, ForeColor = Color.DimGray };
        Controls.Add(footer);

        timer = new System.Windows.Forms.Timer { Interval = 180 };
        timer.Tick += (_, _) => Animate();
        timer.Start();
        RegisterHotKey(Handle, 1001, 0x0008, (int)Keys.U);
        RegisterHotKey(Handle, 1002, 0x0002 | 0x0004, (int)Keys.G);
    }

    private void Animate()
    {
        progressValue = Math.Min(100, progressValue + Random.Shared.Next(0, 3));
        progress.Value = progressValue;
        status.Text = new[]
        {
            "Упорядочиваем вязанку...",
            "Согласовываем с генсухой...",
            "Развлекаем системный Framework...",
            "Проверяем, где гусь...",
            "Ищем OSEMENIT.Bimbim...",
            "Ускоряем Чебурнет...",
            "Спрашиваем клопов, всё ли им нравится..."
        }[Random.Shared.Next(7)];
    }

    private void NextPage()
    {
        page++;
        if (page >= titles.Length)
        {
            timer.Stop();
            progress.Value = 100;
            status.Text = "Готово. Даже гусь это признал.";
            pageTitle.Text = "Установка завершена";
            pageText.Text = "CutVPN установлен.\n\nВнутри этого шуточного проекта предусмотрены сцены про Workrave, Cockroach on Desktop, Desktop Goose и Чебурнет.\n\nЗакройте мастер, чтобы открыть CutVPN в трее.";
            next.Text = "Готово";
            next.Enabled = false;
            return;
        }
        pageTitle.Text = titles[page];
        pageText.Text = texts[page];
        news.Text = page switch
        {
            1 => "Реклама: уничтожение клопов. Услуга не сертифицирована, но очень уверенная.",
            2 => "Объявление: ГУСЬ ПРОДАЁТСЯ. Торг отсутствует. Кнопка «Купить» морально обязательна.",
            3 => "Чебурнет сообщает: вязанка успешно передана генсухе.",
            4 => "Обновление: Framework по доению коровы найден. Осталось доить.",
            5 => "СРОЧНО: гусь всё отрицает.",
            6 => "Компоненты проверены. Почти все. Наверное.",
            _ => "Финальная оптимизация: делаем вид, что всё под контролем."
        };
        progressValue = Math.Min(99, page * 12 + 5);
        progress.Value = progressValue;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0312)
        {
            int id = m.WParam.ToInt32();
            if (id == 1001) Close();
            if (id == 1002) { app.EmergencyStop(); Close(); }
        }
        base.WndProc(ref m);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        timer.Stop();
        UnregisterHotKey(Handle, 1001);
        UnregisterHotKey(Handle, 1002);
        base.OnFormClosed(e);
    }

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

internal sealed class FakePoster : Panel
{
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var bg = new SolidBrush(Color.FromArgb(15, 128, 110));
        e.Graphics.FillRectangle(bg, ClientRectangle);
        e.Graphics.DrawString("WINDOWS", new Font("Tahoma", 23F, FontStyle.Bold), Brushes.White, 16, 14);
        e.Graphics.DrawString("ХУЯК-98 XP/VISTA", new Font("Tahoma", 14F, FontStyle.Bold), Brushes.Yellow, 16, 54);
        using var card = new SolidBrush(Color.FromArgb(240, 240, 240));
        e.Graphics.FillRectangle(card, 16, 95, Width - 32, 112);
        e.Graphics.DrawRectangle(Pens.Navy, 16, 95, Width - 32, 112);
        e.Graphics.DrawString("ВНИМАНИЕ!", new Font("Tahoma", 15F, FontStyle.Bold), Brushes.DarkRed, 28, 110);
        e.Graphics.DrawString("Гусь подключён к Чебурнету.\nВязанка загружена.\nОсеменение в процессе.", new Font("Tahoma", 9F), Brushes.Black, 28, 141);
        e.Graphics.FillEllipse(Brushes.White, Width - 86, 113, 46, 46);
        e.Graphics.FillEllipse(Brushes.DarkGray, Width - 76, 123, 11, 11);
        e.Graphics.FillEllipse(Brushes.DarkGray, Width - 57, 123, 11, 11);
        e.Graphics.DrawLine(Pens.Black, Width - 66, 145, Width - 45, 145);
    }
}
