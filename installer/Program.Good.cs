using System;
using System.Diagnostics;
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
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.Run(new InstallerForm());
    }
}

internal sealed class InstallConfig
{
    public bool Goose { get; set; } = true;
    public bool Cockroach { get; set; } = true;
    public bool Workrave { get; set; } = true;
    public bool Startup { get; set; } = true;
    public bool TelemaxJoke { get; set; } = true;
}

internal static class Paths
{
    public static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CutVPN");
    public static string Config => Path.Combine(Root, "config.json");
    public static string AgentConfig => Path.Combine(Root, "agent.json");
    public static string AppExe => Path.Combine(Root, "CutVPN.exe");
    public static string Joke => Path.Combine(Root, "telemax-joke.txt");
}

internal sealed class InstallerForm : Form
{
    const int WM_HOTKEY = 0x0312;
    const int HOTKEY_EXIT = 7001;
    const int HOTKEY_STOP = 7002;
    const uint MOD_WIN = 0x0008;
    const uint MOD_CTRL_SHIFT = 0x0002 | 0x0004;
    const int INSTALL_SECONDS = 150;

    readonly Label title = new();
    readonly Label subtitle = new();
    readonly Panel content = new();
    readonly ProgressBar progress = new();
    readonly Label progressText = new();
    readonly Button back = new() { Text = "< Назад" };
    readonly Button next = new() { Text = "Далее >" };
    readonly Button cancel = new() { Text = "Отмена" };
    readonly Timer timer = new() { Interval = 1000 };
    readonly CheckBox goose = new() { Text = "Desktop Goose — «ПРОДАМ ГУСЯ» — КУПИТЬ", AutoSize = true, Checked = true };
    readonly CheckBox cockroach = new() { Text = "Cockroach on Desktop — «Уничтожение клопов из дома»", AutoSize = true, Checked = true };
    readonly CheckBox workrave = new() { Text = "Workrave — «улучшение зрения, не вставая из-за ПК»", AutoSize = true, Checked = true };
    readonly CheckBox startup = new() { Text = "Запускать CutVPN при входе в Windows", AutoSize = true, Checked = true };
    readonly CheckBox telemax = new() { Text = "УСТАНОВКА ЭКСТРЕМИСТСКОГО КЛИЕНТА TELEMAX (майор сможет подтереться вашими персональными данными)", AutoSize = true, Checked = true };
    int page;
    int elapsed;
    bool installing;

    readonly string[] fake =
    {
        "Согласовываем вязанку с генсухой...",
        "Ищем OSEMENIT.Bimbim...",
        "Проверяем, где гусь...",
        "Опрашиваем тараканов о лицензии...",
        "Workrave пытается найти зрение...",
        "Загружаем Framework по доению коровы...",
        "Проверяем протокол Чебурнета...",
        "Уточняем у генсухи, можно ли продолжать...",
        "Гусь украл 0.7% прогресса..."
    };

    public InstallerForm()
    {
        Text = "Мастер шиттинга Чебурнета — CutVPN Setup";
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        BackColor = Color.FromArgb(192, 192, 192);
        Font = new Font("Tahoma", 9F);
        KeyPreview = true;

        var top = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.FromArgb(0, 0, 128) };
        top.Controls.Add(new Label { Text = "Мастер шиттинга Чебурнета", ForeColor = Color.White, Font = new Font("Tahoma", 15F, FontStyle.Bold), Location = new Point(18, 14), AutoSize = true });
        var close = new Button { Text = "X", Size = new Size(40, 32), Dock = DockStyle.Right, Font = new Font("Tahoma", 10F, FontStyle.Bold) };
        close.Click += (_, _) => Close();
        top.Controls.Add(close);
        Controls.Add(top);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(14, 10, 14, 0) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 245));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        Controls.Add(layout);

        var left = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(210, 210, 210), BorderStyle = BorderStyle.Fixed3D };
        left.Controls.Add(new Label { Text = "МАСТЕР УСТАНОВКИ\n\n1. Интернет\n2. Персональные предложения\n3. Сясь кран\n4. Компоненты\n5. Новости\n6. Безопасность\n7. Установка\n8. Готово\n\nДиагностика:\nГусь: найден\nТараканы: найдены\nWorkrave: моргает\nГенсуха: онлайн\nВязанка: 97%", Location = new Point(14, 14), Size = new Size(210, 480), Font = new Font("Tahoma", 9F) });
        layout.Controls.Add(left, 0, 0);

        var main = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.Fixed3D, Padding = new Padding(18) };
        layout.Controls.Add(main, 1, 0);

        var head = new Panel { Dock = DockStyle.Top, Height = 90 };
        title.Dock = DockStyle.Top; title.Height = 34; title.Font = new Font("Tahoma", 17F, FontStyle.Bold); title.ForeColor = Color.Navy;
        subtitle.Dock = DockStyle.Fill; subtitle.Font = new Font("Tahoma", 10F);
        head.Controls.Add(subtitle); head.Controls.Add(title);
        main.Controls.Add(head);

        content.Dock = DockStyle.Fill; content.BackColor = Color.FromArgb(238, 238, 238); content.BorderStyle = BorderStyle.Fixed3D; content.AutoScroll = true;
        main.Controls.Add(content);

        var pbar = new Panel { Dock = DockStyle.Bottom, Height = 55, BackColor = Color.White };
        progress.Dock = DockStyle.Top; progress.Height = 18; progress.Maximum = INSTALL_SECONDS; progress.Visible = false;
        progressText.Dock = DockStyle.Bottom; progressText.Height = 22; progressText.Visible = false;
        pbar.Controls.Add(progressText); pbar.Controls.Add(progress); main.Controls.Add(pbar);

        var buttons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, BackColor = Color.FromArgb(212, 212, 212), Padding = new Padding(5, 10, 5, 8) };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        buttons.Controls.Add(new Label { Text = "Win+U / Esc — выйти • Ctrl+Shift+G — аварийный стоп", Dock = DockStyle.Fill, ForeColor = Color.DimGray, Padding = new Padding(5, 6, 0, 0) }, 0, 0);
        foreach (var b in new[] { back, next, cancel }) { b.Dock = DockStyle.Fill; b.Margin = new Padding(4, 0, 4, 8); }
        buttons.Controls.Add(back, 1, 0); buttons.Controls.Add(next, 2, 0); buttons.Controls.Add(cancel, 3, 0);
        layout.Controls.Add(buttons, 0, 1); layout.SetColumnSpan(buttons, 2);

        back.Click += (_, _) => Move(-1);
        next.Click += (_, _) => Move(1);
        cancel.Click += (_, _) => Close();
        timer.Tick += (_, _) => TickInstall();
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape || (e.KeyCode == Keys.U && e.Modifiers == Keys.Windows)) Close(); if (e.KeyCode == Keys.G && e.Control && e.Shift) Close(); };
        Shown += (_, _) => { RegisterHotKey(Handle, HOTKEY_EXIT, MOD_WIN, (int)Keys.U); RegisterHotKey(Handle, HOTKEY_STOP, MOD_CTRL_SHIFT, (int)Keys.G); };
        FormClosed += (_, _) => { UnregisterHotKey(Handle, HOTKEY_EXIT); UnregisterHotKey(Handle, HOTKEY_STOP); timer.Stop(); };
        ShowPage();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && (m.WParam.ToInt32() == HOTKEY_EXIT || m.WParam.ToInt32() == HOTKEY_STOP)) { Close(); return; }
        base.WndProc(ref m);
    }

    void Move(int delta)
    {
        if (installing) return;
        page = Math.Clamp(page + delta, 0, 7);
        if (page == 6) { elapsed = 0; installing = true; progress.Value = 0; timer.Start(); }
        ShowPage();
    }

    void ShowPage()
    {
        content.Controls.Clear();
        progress.Visible = page == 6; progressText.Visible = page == 6;
        back.Enabled = page > 0 && page != 6 && page != 7;
        next.Enabled = page != 6;
        cancel.Enabled = page < 7;
        next.Text = page == 7 ? "Готово" : "Далее >";
        title.Text = new[] { "Добро пожаловать в CutVPN", "Параметры Интернета для локальной сети", "Персональные предложения", "Свойства: сясь кран", "Компоненты CutVPN", "Новости Чебурнета", "Установка CutVPN", "Установка завершена" }[page];
        subtitle.Text = page == 6 ? "Фейковая установка длится ровно 2 минуты 30 секунд." : "Система очень серьёзно относится к происходящему.";

        var p = new Panel { Dock = DockStyle.Top, Height = 540, Padding = new Padding(22), BackColor = Color.FromArgb(242, 242, 242) };
        content.Controls.Add(p);
        switch (page)
        {
            case 0: Welcome(p); break;
            case 1: Internet(p); break;
            case 2: Personal(p); break;
            case 3: Crane(p); break;
            case 4: Components(p); break;
            case 5: News(p); break;
            case 6: Installing(p); break;
            case 7: Finish(p); break;
        }
    }

    static Label T(string text, int x, int y, int w, int h, int size = 10, FontStyle style = FontStyle.Regular) => new() { Text = text, Location = new Point(x, y), Size = new Size(w, h), Font = new Font("Tahoma", size, style) };

    void Welcome(Panel p)
    {
        p.Controls.Add(T("Вас приветствует мастер шиттинга Чебурнета", 18, 18, 820, 42, 17, FontStyle.Bold));
        p.Controls.Add(T("CutVPN подготовит вашу систему к обычному интернету Чебурнета.", 18, 72, 820, 35, 11));
        p.Controls.Add(T("Сегодня мастер поможет установить:\n\n• гусь — с отдельным коммерческим предложением\n• тараканов — с жилищной рекламой\n• Workrave — с улучшением зрения, не вставая\n• CutVPN — с управлением через локальный агент\n• очень важную, но совершенно бесполезную телематику", 18, 120, 520, 210, 11));
        p.Controls.Add(T("СРОЧНАЯ НОВОСТЬ\n\nВЯЗАНКА СНОВА БЫЛА ЗАМЕЧЕНА РЯДОМ С ГЕНСУХОЙ.\n\nГУСЬ ОТКАЗАЛСЯ ДАВАТЬ КОММЕНТАРИИ.", 575, 125, 280, 170, 11, FontStyle.Bold));
        p.Controls.Add(T("Все кнопки настоящие. Смысл — нет.", 18, 395, 600, 30, 10, FontStyle.Italic));
    }

    void Internet(Panel p)
    {
        p.Controls.Add(T("Параметры Интернета для локальной сети", 18, 18, 820, 36, 14, FontStyle.Bold));
        p.Controls.Add(T("Выберите способ настройки параметров прокси-сервера. Использование автоматической настройки рекомендуется.", 18, 62, 820, 50, 10));
        var g = new GroupBox { Text = "Автоматическая настройка", Location = new Point(18, 125), Size = new Size(820, 170) };
        g.Controls.Add(new CheckBox { Text = "Автоматическое определение прокси-сервера (рекомендуется)", Location = new Point(18, 28), AutoSize = true, Checked = true });
        g.Controls.Add(new CheckBox { Text = "Использовать сценарий автоматической настройки", Location = new Point(18, 65), AutoSize = true });
        g.Controls.Add(new Label { Text = "Адрес:", Location = new Point(18, 105), AutoSize = true });
        g.Controls.Add(new TextBox { Location = new Point(76, 102), Width = 570, Text = "http://proxy.cheburetnet.local/auto.pac" });
        p.Controls.Add(g);
        p.Controls.Add(new CheckBox { Text = "Ручная настройка прокси-сервера", Location = new Point(20, 320), AutoSize = true });
        p.Controls.Add(T("Результат:\nСеть: ЧЕБУРНЕТ\nПрокси: найден по праздникам\nГусь: присутствует\nКлопы: требуют лицензию", 20, 365, 500, 130));
    }

    void Personal(Panel p)
    {
        p.Controls.Add(T("Персональные предложения", 18, 18, 820, 36, 14, FontStyle.Bold));
        p.Controls.Add(T("Георгий просит вас предоставить все персональные данные для их обработки и хранения. Вся информация будет храниться до 10380 дней.", 18, 62, 820, 70));
        p.Controls.Add(T("Кто вы по национальности?", 18, 150, 210, 25));
        p.Controls.Add(new TextBox { Text = "Чебурек", Location = new Point(245, 146), Width = 300 });
        p.Controls.Add(T("Количество детей в семье", 18, 200, 210, 25));
        p.Controls.Add(new NumericUpDown { Location = new Point(245, 196), Minimum = 0, Maximum = 99, Value = 1, Width = 80 });
        p.Controls.Add(T("Предложение дня: улучшение зрения, не вставая из-за ПК.", 18, 255, 700, 30, 11, FontStyle.Bold));
        p.Controls.Add(T("Кнопка «Далее» автоматически означает согласие с тем, что вы только что не читали.", 18, 320, 760, 50, 9, FontStyle.Italic));
    }

    void Crane(Panel p)
    {
        p.Controls.Add(T("Свойства: сясь кран", 18, 18, 820, 36, 14, FontStyle.Bold));
        p.Controls.Add(T("ваш sаmsung кран:\nНетрадиционный моне писа нахуй в VBE Miniport - Standard PCI Graphics Adapter (VGA).", 18, 62, 820, 70));
        p.Controls.Add(T("Ваш гендер", 18, 150, 140, 25));
        var gender = new ComboBox { Location = new Point(180, 146), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
        gender.Items.AddRange(new object[] { "АНАНАС SPIR PRO(много)", "Гусь", "Вязанка", "Работает кран" }); gender.SelectedIndex = 0; p.Controls.Add(gender);
        p.Controls.Add(T("Область империи", 18, 205, 140, 25));
        var empire = new ComboBox { Location = new Point(180, 201), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
        empire.Items.AddRange(new object[] { "Империя Чебурнета", "Гусландия", "Вязаночная область", "Территория генсухи" }); empire.SelectedIndex = 0; p.Controls.Add(empire);
        p.Controls.Add(T("Область империи: мало → дохуя", 18, 258, 220, 25));
        p.Controls.Add(new TrackBar { Location = new Point(240, 250), Minimum = 0, Maximum = 100, Value = 70, Width = 380 });
        p.Controls.Add(T("Проверка крана завершена: кран существует.", 18, 335, 700, 30, 10, FontStyle.Bold));
    }

    void Components(Panel p)
    {
        p.Controls.Add(T("Выбор компонентов CutVPN", 18, 18, 820, 36, 14, FontStyle.Bold));
        p.Controls.Add(T("По умолчанию выбрано всё. Каждый компонент устанавливается явно как часть этой установки.", 18, 62, 820, 45));
        goose.Location = new Point(25, 130); p.Controls.Add(goose);
        cockroach.Location = new Point(25, 175); p.Controls.Add(cockroach);
        workrave.Location = new Point(25, 220); p.Controls.Add(workrave);
        startup.Location = new Point(25, 265); p.Controls.Add(startup);
        telemax.Location = new Point(25, 310); p.Controls.Add(telemax);
        p.Controls.Add(T("Workrave → «улучшение зрения, не вставая из-за ПК»\nCockroach → «уничтожение клопов из дома»\nGoose → «ПРОДАМ ГУСЯ» — кнопка только КУПИТЬ\nTELEMAX → чёрная комедия; реальный клиент не устанавливается", 25, 370, 800, 120));
    }

    void News(Panel p)
    {
        p.Controls.Add(T("НОВОСТИ ЧЕБУРНЕТА", 18, 18, 820, 36, 15, FontStyle.Bold));
        p.Controls.Add(T("СРОЧНО!\n\nГенсуха снова согласовала вязанку.\n\nГусь признал свою вину, но попросил адвоката.\n\nОсеменение сети повысило моральный дух на 47%.\n\nFramework по доению коровы обновился и теперь умеет открывать калькулятор.\n\nКлопы объявили себя юридическим лицом.", 22, 75, 820, 310, 12, FontStyle.Bold));
        p.Controls.Add(T("Источник: редакция «Чебурнет сегодня», состоящая из одного гуся.", 22, 410, 800, 30, 9, FontStyle.Italic));
    }

    void Installing(Panel p)
    {
        p.Controls.Add(T("Файлы копируются. Жизненно важные решения принимаются.", 18, 18, 820, 40, 14, FontStyle.Bold));
        p.Controls.Add(T("Реальное время фейковой установки: 2 минуты 30 секунд. В ключевые моменты мастер открывает выбранные локальные установщики из payload.", 18, 65, 820, 55, 11));
        p.Controls.Add(T("CutVPN.exe\nconfig.json\nagent.json\nGoose\nCockroach\nWorkrave\nTELEMAX joke", 25, 135, 330, 220, 12));
        p.Controls.Add(T("Служебные операции:\n\n☑ Проверка лицензии\n☑ Согласование с генсухой\n☑ Вязанка загружена\n☑ OSEMENIT.Bimbim проверен\n☑ Гусь найден\n☑ Тараканы зарегистрированы", 420, 135, 360, 260, 11));
        p.Controls.Add(T("Не закрывайте мастер: он делает вид, что всё под контролем.", 25, 410, 700, 30, 10, FontStyle.Italic));
        progressText.Text = $"{fake[(elapsed / 10) % fake.Length]}   Осталось ~{Math.Max(0, INSTALL_SECONDS - elapsed)} сек.";
    }

    void Finish(Panel p)
    {
        p.Controls.Add(T("УСТАНОВКА ЗАВЕРШЕНА", 18, 18, 820, 45, 18, FontStyle.Bold));
        p.Controls.Add(T($"CutVPN сохранён в:\n{Paths.Root}\n\nКомпоненты:\nDesktop Goose: {(goose.Checked ? "выбран" : "нет")}\nCockroach: {(cockroach.Checked ? "выбран" : "нет")}\nWorkrave: {(workrave.Checked ? "выбран" : "нет")}\nАвтозапуск CutVPN: {(startup.Checked ? "включён" : "выключен")}\nTELEMAX: {(telemax.Checked ? "шутка отмечена" : "не отмечена")}", 25, 90, 800, 300, 12));
        p.Controls.Add(T("Локальный agent.json уже создан для будущего Telegram-бота. Порт по умолчанию: 8765, bind: 127.0.0.1.", 25, 410, 800, 50, 10, FontStyle.Bold));
    }

    void StartComponent(string key)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "payload");
        var files = key switch
        {
            "goose" => new[] { "DesktopGoose.Setup.exe", "DesktopGoose.exe", "DesktopGoose.msi" },
            "cockroach" => new[] { "Cockroach.Setup.exe", "Cockroach.exe", "Cockroach.msi" },
            "workrave" => new[] { "Workrave.Setup.exe", "Workrave.exe", "Workrave.msi" },
            _ => Array.Empty<string>()
        };
        foreach (var f in files)
        {
            var path = Path.Combine(dir, f);
            if (!File.Exists(path)) continue;
            try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true, WorkingDirectory = dir }); }
            catch { }
            return;
        }
    }

    void TickInstall()
    {
        elapsed++;
        progress.Value = elapsed;
        progressText.Text = $"{fake[(elapsed / 10) % fake.Length]}   Осталось ~{Math.Max(0, INSTALL_SECONDS - elapsed)} сек.";
        if (elapsed == 12 && goose.Checked) StartComponent("goose");
        if (elapsed == 62 && cockroach.Checked) StartComponent("cockroach");
        if (elapsed == 112 && workrave.Checked) StartComponent("workrave");
        if (elapsed >= INSTALL_SECONDS)
        {
            timer.Stop();
            installing = false;
            SaveConfig();
            page = 7;
            ShowPage();
        }
    }

    void SaveConfig()
    {
        Directory.CreateDirectory(Paths.Root);
        var cfg = new InstallConfig { Goose = goose.Checked, Cockroach = cockroach.Checked, Workrave = workrave.Checked, Startup = startup.Checked, TelemaxJoke = telemax.Checked };
        File.WriteAllText(Paths.Config, JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Paths.AgentConfig, JsonSerializer.Serialize(new { bind = "127.0.0.1", port = 8765, auth = "SET_LOCAL_SECRET", commands = new[] { "status", "visuals_on", "visuals_off", "random_error", "stop" } }, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Paths.Joke, telemax.Checked ? "TELEMAX — шутливый пункт. Реальный клиент не устанавливается.\r\n" : "");
        var startupFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "CutVPN.cmd");
        if (startup.Checked) File.WriteAllText(startupFile, $"@echo off\r\nstart \"\" \"{Paths.AppExe}\" --installed\r\n"); else if (File.Exists(startupFile)) File.Delete(startupFile);
    }
}
