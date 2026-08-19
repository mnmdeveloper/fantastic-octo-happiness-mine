using System;
using System.Drawing;
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
    public bool Startup { get; set; } = true;
    public string UserText { get; set; } = "Чебурек";
    public int Children { get; set; } = 1;
    public string Empire { get; set; } = "Империя Чебурнета";
}

internal sealed class SetupWizard : Form
{
    private const int HotkeyExit = 41001;
    private const int HotkeyStop = 41002;
    private const uint ModWin = 0x0008;
    private const uint ModCtrlShift = 0x0002 | 0x0004;
    private const int WmHotkey = 0x0312;

    private readonly Panel pageHost;
    private readonly Label pageTitle;
    private readonly Label pageStatus;
    private readonly Label breadcrumb;
    private readonly ProgressBar progress;
    private readonly Button back;
    private readonly Button next;
    private readonly Button cancel;
    private readonly System.Windows.Forms.Timer installTimer;
    private readonly CheckBox goose;
    private readonly CheckBox cockroach;
    private readonly CheckBox workrave;
    private readonly CheckBox startup;
    private readonly TextBox userText;
    private readonly NumericUpDown children;
    private readonly ComboBox empire;
    private readonly TrackBar nonsense;

    private int page;
    private int installValue;
    private bool installing;

    private readonly string[] fakeStatus =
    {
        "Согласовываем вязанку с генсухой...",
        "Ищем OSEMENIT.Bimbim...",
        "Проверяем сертификацию гуся...",
        "Опрашиваем тараканов о лицензии...",
        "Загружаем Framework по доению коровы...",
        "Проверяем Workrave на наличие зрения...",
        "Регистрируем Чебурнет в локальной сети...",
        "Генсуха снова сказала «да»...",
        "Подготавливаем очень важный результат..."
    };

    public SetupWizard()
    {
        Text = "Мастер шиттинга Чебурнета — CutVPN Setup";
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        BackColor = Color.FromArgb(192, 192, 192);
        Font = new Font("Tahoma", 9F);
        KeyPreview = true;
        DoubleBuffered = true;

        var titleBar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(0, 0, 128) };
        Controls.Add(titleBar);
        titleBar.Controls.Add(new Label { Text = "Мастер шиттинга Чебурнета", ForeColor = Color.White, Font = new Font("Tahoma", 15F, FontStyle.Bold), Location = new Point(18, 13), AutoSize = true });
        var close = new Button { Text = "X", Size = new Size(40, 32), Anchor = AnchorStyles.Top | AnchorStyles.Right, Font = new Font("Tahoma", 10F, FontStyle.Bold) };
        close.Click += (_, _) => Close();
        titleBar.Controls.Add(close);
        titleBar.Resize += (_, _) => close.Location = new Point(titleBar.Width - 50, 10);

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 78, BackColor = Color.FromArgb(212, 212, 212) };
        Controls.Add(footer);
        back = new Button { Text = "< Назад", Size = new Size(112, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        next = new Button { Text = "Далее >", Size = new Size(112, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        cancel = new Button { Text = "Отмена", Size = new Size(112, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        footer.Controls.Add(back);
        footer.Controls.Add(next);
        footer.Controls.Add(cancel);
        footer.Resize += (_, _) =>
        {
            cancel.Location = new Point(footer.Width - 126, 18);
            next.Location = new Point(footer.Width - 246, 18);
            back.Location = new Point(footer.Width - 366, 18);
        };
        back.Click += (_, _) => Navigate(-1);
        next.Click += (_, _) => Navigate(1);
        cancel.Click += (_, _) => Close();

        var help = new Label { Text = "Win+U — выйти    •    Ctrl+Shift+G — аварийный стоп", Dock = DockStyle.Bottom, Height = 22, Padding = new Padding(12, 2, 0, 0), BackColor = Color.FromArgb(212, 212, 212), ForeColor = Color.DimGray };
        Controls.Add(help);

        var baseArea = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 8), BackColor = Color.FromArgb(192, 192, 192) };
        Controls.Add(baseArea);

        var header = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = Color.FromArgb(245, 245, 245), BorderStyle = BorderStyle.Fixed3D };
        baseArea.Controls.Add(header);
        pageTitle = new Label { Location = new Point(18, 12), AutoSize = true, Font = new Font("Tahoma", 16F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 0, 128) };
        header.Controls.Add(pageTitle);
        pageStatus = new Label { Location = new Point(18, 46), AutoSize = true, ForeColor = Color.DimGray };
        header.Controls.Add(pageStatus);

        var main = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.Fixed3D };
        baseArea.Controls.Add(main);

        var side = new Panel { Dock = DockStyle.Left, Width = 235, BackColor = Color.FromArgb(210, 210, 210), BorderStyle = BorderStyle.Fixed3D, Padding = new Padding(12) };
        main.Controls.Add(side);
        side.Controls.Add(new Label { Text = "Мастер установки", AutoSize = true, Font = new Font("Tahoma", 10F, FontStyle.Bold), Location = new Point(10, 10) });
        breadcrumb = new Label { Location = new Point(10, 45), Size = new Size(195, 390), Font = new Font("Tahoma", 9F) };
        side.Controls.Add(breadcrumb);
        side.Controls.Add(new Label { Text = "Служебная диагностика:\n\nГусь: найден\nКлопы: обнаружены\nWorkrave: моргает\nГенсуха: доступна\nВязанка: 97%", Location = new Point(10, 445), Size = new Size(205, 100), ForeColor = Color.Navy });

        var right = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18) };
        main.Controls.Add(right);
        pageHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(238, 238, 238), BorderStyle = BorderStyle.Fixed3D, AutoScroll = true };
        right.Controls.Add(pageHost);

        progress = new ProgressBar { Dock = DockStyle.Bottom, Height = 23, Minimum = 0, Maximum = 100, Visible = false };
        right.Controls.Add(progress);

        goose = new CheckBox { Text = "Desktop Goose — гусь официально одобрен", AutoSize = true, Checked = true };
        cockroach = new CheckBox { Text = "Cockroach on Desktop — анти-клоповая защита", AutoSize = true, Checked = true };
        workrave = new CheckBox { Text = "Workrave — улучшение зрения не вставая", AutoSize = true, Checked = true };
        startup = new CheckBox { Text = "Запускать CutVPN при входе в Windows", AutoSize = true, Checked = true };
        userText = new TextBox { Text = "Чебурек", Width = 280 };
        children = new NumericUpDown { Minimum = 0, Maximum = 99, Value = 1, Width = 80 };
        empire = new ComboBox { Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
        empire.Items.AddRange(new object[] { "Империя Чебурнета", "Гусландия", "Вязаночная область", "Территория генсухи" });
        empire.SelectedIndex = 0;
        nonsense = new TrackBar { Minimum = 0, Maximum = 100, Value = 70, TickFrequency = 10, Width = 360 };

        installTimer = new System.Windows.Forms.Timer { Interval = 180 };
        installTimer.Tick += (_, _) => InstallTick();

        Shown += (_, _) =>
        {
            RegisterHotKey(Handle, HotkeyExit, ModWin, (int)Keys.U);
            RegisterHotKey(Handle, HotkeyStop, ModCtrlShift, (int)Keys.G);
            Render();
        };
        Render();
    }

    private void Navigate(int delta)
    {
        if (installing) return;
        page = Math.Clamp(page + delta, 0, 7);
        if (page == 6) StartInstall(); else Render();
    }

    private void Render()
    {
        pageHost.Controls.Clear();
        progress.Visible = page == 6;
        back.Enabled = page > 0 && page != 6 && page != 7;
        next.Enabled = page != 6;
        cancel.Enabled = page < 7 && !installing;
        next.Text = page == 7 ? "Готово" : "Далее >";
        pageTitle.Text = page switch
        {
            0 => "Параметры Интернета для локальной сети",
            1 => "Персональные предложения",
            2 => "Свойства: сясь кран",
            3 => "Компоненты шиттинга",
            4 => "Новости Чебурнета",
            5 => "Проверка системной безопасности",
            6 => "Установка CutVPN",
            _ => "Установка завершена"
        };
        pageStatus.Text = page switch
        {
            0 => "Выберите способ настройки. Система уже считает себя администратором.",
            1 => "Не волнуйтесь: это абсолютно обязательные персональные предложения.",
            2 => "Параметр «дофуя» применён успешно.",
            3 => "Компоненты перечислены. Каждый из них жизненно необходим.",
            4 => "Новости обновлены 0 секунд назад.",
            5 => "Проверка завершена на 10380%. Это хороший знак.",
            6 => "Идёт очень важная установка.",
            _ => "Мастер завершил процедуру. Гусь уведомлён."
        };
        breadcrumb.Text = $"1. Интернет{(page == 0 ? "  ←" : "")}\n2. Персональные предложения{(page == 1 ? "  ←" : "")}\n3. Сясь кран{(page == 2 ? "  ←" : "")}\n4. Компоненты{(page == 3 ? "  ←" : "")}\n5. Новости{(page == 4 ? "  ←" : "")}\n6. Безопасность{(page == 5 ? "  ←" : "")}\n7. Установка{(page == 6 ? "  ←" : "")}\n8. Готово{(page == 7 ? "  ←" : "")}";

        var panel = new Panel { Dock = DockStyle.Top, Height = 540, BackColor = Color.FromArgb(242, 242, 242), Padding = new Padding(20) };
        pageHost.Controls.Add(panel);
        switch (page)
        {
            case 0: Internet(panel); break;
            case 1: Personal(panel); break;
            case 2: Crane(panel); break;
            case 3: Components(panel); break;
            case 4: News(panel); break;
            case 5: Security(panel); break;
            case 6: Install(panel); break;
            default: Finish(panel); break;
        }
    }

    private static Label Txt(string text, int x, int y, int w, int h, int size = 10, FontStyle style = FontStyle.Regular) => new() { Text = text, Location = new Point(x, y), Size = new Size(w, h), Font = new Font("Tahoma", size, style) };

    private void Internet(Panel p)
    {
        p.Controls.Add(Txt("Параметры Интернета для локальной сети", 12, 12, 760, 34, 13, FontStyle.Bold));
        p.Controls.Add(Txt("Если вы не знаете, что выбрать — выбирайте всё. Это исторически проверенный метод.", 12, 55, 780, 45));
        var g = new GroupBox { Text = "Автоматическая настройка", Location = new Point(12, 110), Size = new Size(780, 180) };
        g.Controls.Add(new CheckBox { Text = "Автоматическое определение прокси-сервера (рекомендуется)", Location = new Point(18, 28), AutoSize = true, Checked = true });
        g.Controls.Add(new CheckBox { Text = "Использовать сценарий автоматической настройки", Location = new Point(18, 65), AutoSize = true });
        g.Controls.Add(new Label { Text = "Адрес:", Location = new Point(18, 105), AutoSize = true });
        g.Controls.Add(new TextBox { Text = "http://proxy.cheburetnet.local/auto.pac", Location = new Point(74, 102), Width = 540 });
        p.Controls.Add(g);
        p.Controls.Add(new CheckBox { Text = "Ручная настройка прокси-сервера", Location = new Point(20, 308), AutoSize = true });
        p.Controls.Add(Txt("Результат диагностики:\nСеть: ЧЕБУРНЕТ\nПрокси: найден\nГусь: присутствует\nЛицензия тараканов: сомнительна", 20, 350, 500, 120));
    }

    private void Personal(Panel p)
    {
        p.Controls.Add(Txt("Персональные предложения", 12, 12, 700, 35, 14, FontStyle.Bold));
        p.Controls.Add(Txt("Георгий просит предоставить абсолютно необходимые данные. Обработка обещана до 10380 дней.", 12, 55, 790, 55));
        p.Controls.Add(Txt("Кто вы по национальности?", 12, 130, 190, 25)); userText.Location = new Point(215, 126); p.Controls.Add(userText);
        p.Controls.Add(Txt("Количество детей в семье", 12, 180, 190, 25)); children.Location = new Point(215, 176); p.Controls.Add(children);
        p.Controls.Add(Txt("Ваш любимый гусь", 12, 230, 190, 25)); p.Controls.Add(new TextBox { Text = "Гусь", Location = new Point(215, 226), Width = 280 });
        p.Controls.Add(Txt("Предложение дня: улучшение зрения не вставая из-за ПК — БЕСПЛАТНО.", 12, 285, 740, 35, 10, FontStyle.Bold));
        p.Controls.Add(Txt("Кнопка «Далее» означает, что вы всё поняли и добровольно согласились с тем, чего не читали.", 12, 340, 740, 55, 9, FontStyle.Italic));
    }

    private void Crane(Panel p)
    {
        p.Controls.Add(Txt("Свойства: сясь кран", 12, 12, 700, 35, 14, FontStyle.Bold));
        p.Controls.Add(Txt("ваш sаmsung кран:\nНетрадиционный моне писа нахуй в VBE Miniport - Standard PCI Graphics Adapter (VGA).", 12, 55, 760, 60));
        p.Controls.Add(Txt("Ваш гендер", 12, 135, 180, 25));
        var gender = new ComboBox { Location = new Point(215, 131), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList }; gender.Items.AddRange(new object[] { "АНАНАС SPIR PRO(много)", "Гусь", "Вязанка", "Работает кран" }); gender.SelectedIndex = 0; p.Controls.Add(gender);
        p.Controls.Add(Txt("Область империи", 12, 185, 180, 25)); empire.Location = new Point(215, 181); p.Controls.Add(empire);
        p.Controls.Add(Txt("Степень империи: МАЛО → ДОХУЯ", 12, 235, 260, 25)); nonsense.Location = new Point(215, 225); p.Controls.Add(nonsense);
        p.Controls.Add(Txt("Проверка крана завершена: кран существует.", 12, 300, 600, 30, 10, FontStyle.Bold));
        p.Controls.Add(Txt("Дополнительно:\n☑ нажать кнопку\n☑ посмотреть на кран\n☐ понять происходящее", 12, 350, 500, 110));
    }

    private void Components(Panel p)
    {
        p.Controls.Add(Txt("Компоненты шиттинга", 12, 12, 700, 35, 14, FontStyle.Bold));
        goose.Location = new Point(20, 70); p.Controls.Add(goose); cockroach.Location = new Point(20, 115); p.Controls.Add(cockroach); workrave.Location = new Point(20, 160); p.Controls.Add(workrave); startup.Location = new Point(20, 205); p.Controls.Add(startup);
        p.Controls.Add(Txt("РЕКЛАМА\n\nWorkrave: «улучшение зрения, не вставая из-за ПК»\nCockroach: «уничтожение клопов из дома»\nGoose: «ПРОДАМ ГУСЯ» — кнопка только одна: КУПИТЬ", 20, 270, 740, 150, 11, FontStyle.Bold));
        p.Controls.Add(Txt("Компоненты CutVPN подготавливаются из локальной папки payload. Сторонние программы не скачиваются автоматически из интернета.", 20, 440, 740, 55, 9, FontStyle.Italic));
    }

    private void News(Panel p)
    {
        p.Controls.Add(Txt("НОВОСТИ ЧЕБУРНЕТА — ВЫПУСК 98ΞP", 12, 12, 760, 35, 14, FontStyle.Bold));
        p.Controls.Add(Txt("СРОЧНО!\n\nГенсуха снова заметила вязанку возле сервера.\n\nГусь заявил, что ничего не видел. Свидетели утверждают обратное.\n\nУчёные подтвердили: осеменение сети повышает её моральный дух на 47%.\n\nFramework по доению коровы получил обновление и теперь умеет открывать калькулятор.", 18, 70, 750, 300, 12));
        p.Controls.Add(Txt("Реклама дня: «Продам гуся. Недорого. Кнопка только КУПИТЬ.»", 18, 400, 750, 55, 10, FontStyle.Bold));
    }

    private void Security(Panel p)
    {
        p.Controls.Add(Txt("Проверка системной безопасности", 12, 12, 760, 35, 14, FontStyle.Bold));
        p.Controls.Add(Txt("Проверка завершена:\n\n☑ Framework по доению коровы\n☑ OSEMENIT.Bimbim\n☑ GENSUHA.dll\n☑ VYAZANKA.sys\n☑ Протокол гуся\n☑ Реестр клопов\n\nСистема сообщает: всё выглядит подозрительно, но это нормально.", 18, 72, 740, 280, 12));
        p.Controls.Add(Txt("Нажмите «Далее», чтобы начать установку. На следующем экране начнётся самое серьёзное.", 18, 390, 740, 60, 10, FontStyle.Bold));
    }

    private void Install(Panel p)
    {
        p.Controls.Add(Txt("Файлы копируются. Гусь наблюдает.", 12, 12, 760, 35, 14, FontStyle.Bold));
        p.Controls.Add(Txt("CutVPN.exe\ncheburetnet.cfg\nvyazanka.dat\nOSEMENIT.Bimbim\ngus-report.txt\ncockroach-license.txt\nworkrave-vision.txt", 18, 72, 500, 230, 12));
        p.Controls.Add(Txt("Ожидаем ответа от Чебурнета...\nНе выключайте мастер, пока он делает вид, что считает проценты.", 18, 330, 740, 80, 11, FontStyle.Italic));
    }

    private void Finish(Panel p)
    {
        p.Controls.Add(Txt("УСТАНОВКА ЗАВЕРШЕНА", 12, 12, 760, 40, 18, FontStyle.Bold));
        p.Controls.Add(Txt("CutVPN установлен локально.\n\nГусь: подготовлен\nТараканы: подготовлены\nWorkrave: подготовлен\nАвтозапуск CutVPN: " + (startup.Checked ? "ВКЛ" : "ВЫКЛ") + "\n\nГенсуха довольна. Вязанка сохранена.", 18, 80, 740, 250, 12));
        p.Controls.Add(Txt("Нажмите «Готово», чтобы закрыть мастер. Пранк-компоненты можно отключить локально.", 18, 360, 740, 70, 10, FontStyle.Bold));
    }

    private void StartInstall()
    {
        installing = true;
        installValue = 0;
        progress.Value = 0;
        installTimer.Start();
        Render();
    }

    private void InstallTick()
    {
        installValue = Math.Min(100, installValue + (installValue < 75 ? 1 : 2));
        progress.Value = installValue;
        pageStatus.Text = fakeStatus[(installValue / 5) % fakeStatus.Length];
        if (installValue >= 100)
        {
            installTimer.Stop();
            PerformInstall();
            installing = false;
            page = 7;
            Render();
        }
    }

    private void PerformInstall()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CutVPN");
        Directory.CreateDirectory(root);
        var cfg = new SetupConfig { Goose = goose.Checked, Cockroach = cockroach.Checked, Workrave = workrave.Checked, Startup = startup.Checked, UserText = userText.Text, Children = (int)children.Value, Empire = empire.Text };
        File.WriteAllText(Path.Combine(root, "config.json"), JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(root, "installed-components.txt"), "CutVPN\nGoose=" + goose.Checked + "\nCockroach=" + cockroach.Checked + "\nWorkrave=" + workrave.Checked + "\n");
        if (startup.Checked)
        {
            using var run = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            run?.SetValue("CutVPN", Path.Combine(root, "CutVPN.exe"));
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey)
        {
            var id = m.WParam.ToInt32();
            if (id == HotkeyExit || id == HotkeyStop) { Close(); return; }
        }
        base.WndProc(ref m);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        installTimer.Stop();
        UnregisterHotKey(Handle, HotkeyExit);
        UnregisterHotKey(Handle, HotkeyStop);
        base.OnFormClosed(e);
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
