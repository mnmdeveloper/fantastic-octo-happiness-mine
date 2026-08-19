using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CutVPN.Setup;

internal sealed class SetupState
{
    public bool Goose { get; set; } = true;
    public bool Cockroach { get; set; } = true;
    public bool Workrave { get; set; } = true;
    public bool Startup { get; set; } = true;
    public bool TelemaxJoke { get; set; }
}

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.Run(new SetupForm());
    }
}

internal sealed class SetupForm : Form
{
    private readonly Panel host;
    private readonly Label title;
    private readonly Label status;
    private readonly ProgressBar progress;
    private readonly Button back;
    private readonly Button next;
    private readonly Button cancel;
    private readonly System.Windows.Forms.Timer timer;
    private readonly CheckBox goose;
    private readonly CheckBox cockroach;
    private readonly CheckBox workrave;
    private readonly CheckBox startup;
    private readonly CheckBox telemax;
    private int page;
    private int install;
    private bool installing;

    private static readonly string[] Pages =
    {
        "Параметры Интернета для локальной сети",
        "Персональные предложения",
        "Свойства: сясь кран",
        "Компоненты",
        "Новости Чебурнета",
        "Проверка системной безопасности",
        "Установка CutVPN",
        "Установка завершена"
    };

    private static readonly string[] FakeStatus =
    {
        "Согласовываем вязанку с генсухой...",
        "Ищем OSEMENIT.Bimbim...",
        "Проверяем сертификацию гуся...",
        "Опрашиваем тараканов о лицензии...",
        "Загружаем Framework по доению коровы...",
        "Workrave проверяет, существует ли зрение...",
        "Регистрируем Чебурнет в локальной сети...",
        "Генсуха снова сказала «да»..."
    };

    public SetupForm()
    {
        Text = "Мастер шиттинга Чебурнета — CutVPN Setup";
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        BackColor = Color.FromArgb(192, 192, 192);
        Font = new Font("Tahoma", 9F);
        KeyPreview = true;

        var top = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Navy };
        top.Controls.Add(new Label { Text = "Мастер шиттинга Чебурнета", ForeColor = Color.White, Font = new Font("Tahoma", 15F, FontStyle.Bold), Location = new Point(18, 13), AutoSize = true });
        var close = new Button { Text = "X", Size = new Size(40, 32), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        close.Click += (_, _) => Close();
        top.Controls.Add(close);
        top.Resize += (_, _) => close.Location = new Point(top.Width - 50, 10);
        Controls.Add(top);

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 78, BackColor = Color.FromArgb(212, 212, 212) };
        back = new Button { Text = "< Назад", Size = new Size(112, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        next = new Button { Text = "Далее >", Size = new Size(112, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        cancel = new Button { Text = "Отмена", Size = new Size(112, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        bottom.Controls.Add(back); bottom.Controls.Add(next); bottom.Controls.Add(cancel);
        bottom.Resize += (_, _) => { cancel.Location = new Point(bottom.Width - 126, 18); next.Location = new Point(bottom.Width - 246, 18); back.Location = new Point(bottom.Width - 366, 18); };
        back.Click += (_, _) => Navigate(-1); next.Click += (_, _) => Navigate(1); cancel.Click += (_, _) => Close();
        Controls.Add(bottom);

        host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16), BackColor = Color.FromArgb(192, 192, 192) };
        Controls.Add(host);

        var header = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = Color.White, BorderStyle = BorderStyle.Fixed3D };
        title = new Label { Location = new Point(18, 12), AutoSize = true, Font = new Font("Tahoma", 16F, FontStyle.Bold), ForeColor = Color.Navy };
        status = new Label { Location = new Point(18, 46), AutoSize = true, ForeColor = Color.DimGray };
        header.Controls.Add(title); header.Controls.Add(status);
        host.Controls.Add(header);

        var work = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.Fixed3D, Padding = new Padding(18) };
        host.Controls.Add(work);
        var side = new Label { Dock = DockStyle.Left, Width = 220, Text = "Мастер установки\n\n1. Интернет\n2. Персональные предложения\n3. Сясь кран\n4. Компоненты\n5. Новости\n6. Безопасность\n7. Установка\n8. Готово\n\nДиагностика:\nГусь: найден\nКлопы: найдены\nWorkrave: жив\nВязанка: 97%", BackColor = Color.FromArgb(211, 211, 211), Padding = new Padding(12), BorderStyle = BorderStyle.Fixed3D };
        work.Controls.Add(side);
        host.BringToFront();
        work.BringToFront();
        host.Controls.SetChildIndex(header, 0);

        var right = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18) };
        work.Controls.Add(right);
        host.Controls.SetChildIndex(work, 0);
        host.Controls.SetChildIndex(header, 1);
        right.BringToFront();

        host.Resize += (_, _) => { };
        var shell = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        right.Controls.Add(shell);
        host.Controls.SetChildIndex(work, 0);
        shell.BringToFront();

        // Move title content into shell while preserving the classic layout.
        var content = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(238, 238, 238), BorderStyle = BorderStyle.Fixed3D, AutoScroll = true };
        shell.Controls.Add(content);
        progress = new ProgressBar { Dock = DockStyle.Bottom, Height = 22, Minimum = 0, Maximum = 100, Visible = false };
        shell.Controls.Add(progress);

        goose = new CheckBox { Text = "Desktop Goose — «ПРОДАМ ГУСЯ»", AutoSize = true, Checked = true };
        cockroach = new CheckBox { Text = "Cockroach on Desktop — «Уничтожение клопов из дома»", AutoSize = true, Checked = true };
        workrave = new CheckBox { Text = "Workrave — «улучшение зрения, не вставая из-за ПК»", AutoSize = true, Checked = true };
        startup = new CheckBox { Text = "Запускать CutVPN при входе в Windows", AutoSize = true, Checked = true };
        telemax = new CheckBox { Text = "Установка экстремистского клиента TELEMAX (майор сможет подтереться вашими персональными данными)", AutoSize = true, Checked = false };
        timer = new System.Windows.Forms.Timer { Interval = 180 };
        timer.Tick += (_, _) => InstallTick();

        KeyDown += (_, e) => { if (e.KeyCode == Keys.U && e.Modifiers == Keys.LWin) Close(); if (e.KeyCode == Keys.G && e.Control && e.Shift) Close(); };
        Render(content);
    }

    private void Navigate(int d)
    {
        if (installing) return;
        page = Math.Clamp(page + d, 0, Pages.Length - 1);
        if (page == 6) StartInstall(); else Render((Panel)FindControl(typeof(Panel), 0)!);
    }

    private Control? FindControl(Type type, int dummy) => Controls.OfType<Panel>().FirstOrDefault(p => p.Controls.OfType<Panel>().Any());

    private void Render(Panel content)
    {
        content.Controls.Clear();
        title.Text = Pages[page];
        status.Text = page == 6 ? "Идёт очень важная установка." : "Выберите Далее, чтобы продолжить абсолютно серьёзную процедуру.";
        progress.Visible = page == 6;
        back.Enabled = page > 0 && page != 6 && page != 7;
        next.Enabled = page != 6;
        cancel.Enabled = page < 7 && !installing;
        next.Text = page == 7 ? "Готово" : "Далее >";

        var panel = new Panel { Dock = DockStyle.Top, Height = 520, Padding = new Padding(24), BackColor = Color.FromArgb(242, 242, 242) };
        content.Controls.Add(panel);
        if (page == 0) Internet(panel);
        else if (page == 1) Personal(panel);
        else if (page == 2) Crane(panel);
        else if (page == 3) Components(panel);
        else if (page == 4) News(panel);
        else if (page == 5) Security(panel);
        else if (page == 6) Install(panel);
        else Finish(panel);
    }

    private static Label T(string text, int x, int y, int w, int h, int size = 10, FontStyle style = FontStyle.Regular) => new() { Text = text, Location = new Point(x, y), Size = new Size(w, h), Font = new Font("Tahoma", size, style) };

    private void Internet(Panel p)
    {
        p.Controls.Add(T("Параметры Интернета для локальной сети", 15, 15, 760, 35, 14, FontStyle.Bold));
        p.Controls.Add(T("Выберите способ настройки прокси-сервера. Если не знаете, что выбрать — выберите всё.", 15, 60, 780, 40));
        var g = new GroupBox { Text = "Автоматическая настройка", Location = new Point(15, 110), Size = new Size(780, 180) };
        g.Controls.Add(new CheckBox { Text = "Автоматическое определение прокси-сервера (рекомендуется)", Location = new Point(18, 30), AutoSize = true, Checked = true });
        g.Controls.Add(new CheckBox { Text = "Использовать сценарий автоматической настройки", Location = new Point(18, 70), AutoSize = true });
        g.Controls.Add(new Label { Text = "Адрес:", Location = new Point(18, 110), AutoSize = true });
        g.Controls.Add(new TextBox { Text = "http://proxy.cheburetnet.local/auto.pac", Location = new Point(74, 107), Width = 540 });
        p.Controls.Add(g);
        p.Controls.Add(T("Сеть успешно распознана как: ЛОКАЛЬНАЯ СЕТЬ ГУСЯ\nПрокси: существует по праздникам\nКлопы: требуют лицензию", 20, 330, 700, 100));
    }

    private void Personal(Panel p)
    {
        p.Controls.Add(T("Персональные предложения", 15, 15, 760, 35, 14, FontStyle.Bold));
        p.Controls.Add(T("Георгий просит вас предоставить все персональные данные для обработки и хранения.\nВся информация будет храниться до 10380 дней.", 15, 60, 760, 70));
        p.Controls.Add(T("Кто вы по национальности?", 15, 145, 200, 25));
        var box = new TextBox { Text = "Чебурек", Location = new Point(220, 141), Width = 280 }; p.Controls.Add(box);
        p.Controls.Add(T("Количество детей в семье", 15, 195, 200, 25));
        p.Controls.Add(new NumericUpDown { Location = new Point(220, 191), Minimum = 0, Maximum = 99, Value = 1, Width = 80 });
        p.Controls.Add(T("Предложение дня: улучшение зрения, не вставая из-за ПК.", 15, 260, 700, 35, 11, FontStyle.Bold));
        p.Controls.Add(T("Кнопка «Далее» автоматически означает согласие с тем, что вы только что прочитали.", 15, 320, 720, 55, 9, FontStyle.Italic));
    }

    private void Crane(Panel p)
    {
        p.Controls.Add(T("Свойства: сясь кран", 15, 15, 760, 35, 14, FontStyle.Bold));
        p.Controls.Add(T("ваш sаmsung кран:\nНетрадиционный моне писа нахуй в VBE Miniport - Standard PCI Graphics Adapter (VGA).", 15, 60, 760, 70));
        p.Controls.Add(T("Ваш гендер", 15, 155, 150, 25));
        var gender = new ComboBox { Location = new Point(180, 151), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
        gender.Items.AddRange(new object[] { "АНАНАС SPIR PRO(много)", "Гусь", "Вязанка", "Работает кран" }); gender.SelectedIndex = 0; p.Controls.Add(gender);
        p.Controls.Add(T("Область империи", 15, 210, 150, 25));
        var emp = new ComboBox { Location = new Point(180, 206), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList }; emp.Items.AddRange(new object[] { "Империя Чебурнета", "Гусландия", "Вязаночная область" }); emp.SelectedIndex = 0; p.Controls.Add(emp);
        p.Controls.Add(T("мало → дохуя", 15, 265, 150, 25));
        p.Controls.Add(new TrackBar { Location = new Point(180, 255), Minimum = 0, Maximum = 100, Value = 70, Width = 360 });
        p.Controls.Add(T("Проверка крана завершена: кран существует.", 15, 340, 700, 30, 10, FontStyle.Bold));
    }

    private void Components(Panel p)
    {
        p.Controls.Add(T("Компоненты CutVPN", 15, 15, 760, 35, 14, FontStyle.Bold));
        p.Controls.Add(T("Каждая следующая галочка запускает отдельный, видимый компонентный шаг. Никакой маскировки.", 15, 55, 760, 45));
        goose.Location = new Point(30, 115); p.Controls.Add(goose);
        cockroach.Location = new Point(30, 160); p.Controls.Add(cockroach);
        workrave.Location = new Point(30, 205); p.Controls.Add(workrave);
        startup.Location = new Point(30, 250); p.Controls.Add(startup);
        telemax.Location = new Point(30, 295); p.Controls.Add(telemax);
        p.Controls.Add(T("Реклама: Workrave — зрение; Cockroach — клопы; Goose — только КУПИТЬ.", 30, 360, 740, 60, 10, FontStyle.Italic));
    }

    private void News(Panel p)
    {
        p.Controls.Add(T("НОВОСТИ ЧЕБУРНЕТА", 15, 15, 760, 35, 14, FontStyle.Bold));
        p.Controls.Add(T("СРОЧНО! Генсуха снова увидела вязанку рядом с сервером.\n\nГусь всё отрицает.\n\nУчёные установили: осеменение повышает моральный дух сети на 47%.\n\nFramework по доению коровы получил обновление.", 20, 75, 740, 250, 12));
        p.Controls.Add(T("РЕКЛАМА: «Продам гуся. Кнопка только КУПИТЬ.»", 20, 365, 740, 50, 11, FontStyle.Bold));
    }

    private void Security(Panel p)
    {
        p.Controls.Add(T("Проверка системной безопасности", 15, 15, 760, 35, 14, FontStyle.Bold));
        p.Controls.Add(T("☑ Framework по доению коровы\n☑ OSEMENIT.Bimbim\n☑ GENSUHA.dll\n☑ VYAZANKA.sys\n☑ Протокол гуся\n☑ Реестр клопов", 25, 80, 700, 260, 12));
        p.Controls.Add(T("Проверка сообщает: всё выглядит подозрительно, но это нормально. Нажмите «Далее». ", 25, 370, 700, 50, 10, FontStyle.Bold));
    }

    private void Install(Panel p)
    {
        p.Controls.Add(T("Файлы копируются. Жизненно важные решения принимаются.", 15, 15, 760, 35, 14, FontStyle.Bold));
        p.Controls.Add(T("CutVPN.exe\nconfig.json\nprank.state\nagent.json\ncomponent manifests", 25, 80, 500, 200, 12));
        p.Controls.Add(T("Ожидаем ответа от Чебурнета...\nНе выключайте мастер, пока он делает вид, что считает проценты.", 25, 320, 740, 90, 11, FontStyle.Italic));
    }

    private void Finish(Panel p)
    {
        p.Controls.Add(T("УСТАНОВКА ЗАВЕРШЕНА", 15, 15, 760, 40, 18, FontStyle.Bold));
        p.Controls.Add(T("CutVPN установлен.\n\nГусь: " + (goose.Checked ? "выбран" : "пропущен") + "\nТараканы: " + (cockroach.Checked ? "выбраны" : "пропущены") + "\nWorkrave: " + (workrave.Checked ? "выбран" : "пропущен") + "\nАвтозапуск: " + (startup.Checked ? "ВКЛ" : "ВЫКЛ") + "\nTelemax joke: " + (telemax.Checked ? "активирован" : "нет"), 20, 85, 740, 260, 12));
        p.Controls.Add(T("Локальный интерфейс для будущего Telegram-бота подготовлен: config.json + agent.json. Внешние удалённые команды здесь не запускаются.", 20, 370, 740, 65, 10, FontStyle.Bold));
    }

    private void StartInstall()
    {
        installing = true; install = 0; progress.Value = 0; timer.Start(); Render((Panel)FindControl(typeof(Panel), 0)!);
    }

    private void InstallTick()
    {
        install = Math.Min(100, install + (install < 75 ? 1 : 2));
        progress.Value = install;
        status.Text = FakeStatus[(install / 5) % FakeStatus.Length];
        if (install >= 100)
        {
            timer.Stop(); PerformInstall(); installing = false; page = 7; Render((Panel)FindControl(typeof(Panel), 0)!);
        }
    }

    private void PerformInstall()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CutVPN");
        Directory.CreateDirectory(root);
        var state = new SetupState { Goose = goose.Checked, Cockroach = cockroach.Checked, Workrave = workrave.Checked, Startup = startup.Checked, TelemaxJoke = telemax.Checked };
        File.WriteAllText(Path.Combine(root, "config.json"), JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(root, "agent.json"), JsonSerializer.Serialize(new { protocol = "cutvpn-local-v1", endpoint = "127.0.0.1", port = 48761, commands = new[] { "status", "visuals_on", "visuals_off", "random_error", "uninstall" } }, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(root, "installed-components.txt"), $"Goose={goose.Checked}\nCockroach={cockroach.Checked}\nWorkrave={workrave.Checked}\nTelemaxJoke={telemax.Checked}\n");

        var payload = Path.Combine(AppContext.BaseDirectory, "payload");
        var installedPayload = Path.Combine(root, "payload");
        if (Directory.Exists(payload))
        {
            Directory.CreateDirectory(installedPayload);
            foreach (var file in Directory.GetFiles(payload)) File.Copy(file, Path.Combine(installedPayload, Path.GetFileName(file)), true);
        }

        InstallPayload("Goose", goose.Checked, "DesktopGoose.Setup.exe", root);
        InstallPayload("Cockroach", cockroach.Checked, "Cockroach.Setup.exe", root);
        InstallPayload("Workrave", workrave.Checked, "Workrave.Setup.exe", root);
        if (startup.Checked)
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            key?.SetValue("CutVPN", Path.Combine(root, "CutVPN.exe"));
        }
        if (telemax.Checked) File.WriteAllText(Path.Combine(root, "telemax-joke.txt"), "Это шутка. Реальный клиент TELEMAX не устанавливается.");
    }

    private static void InstallPayload(string name, bool selected, string exe, string root)
    {
        if (!selected) return;
        var path = Path.Combine(root, "payload", exe);
        if (File.Exists(path))
        {
            try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); } catch { }
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        timer.Stop();
        base.OnFormClosed(e);
    }
}
