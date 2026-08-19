using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CutVPN.Setup;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new SetupWizard());
    }
}

internal sealed class SetupWizard : Form
{
    private readonly Panel content;
    private readonly Label title;
    private readonly Label subtitle;
    private readonly Button back;
    private readonly Button next;
    private readonly Button cancel;
    private readonly ProgressBar installBar;
    private readonly Label installStatus;
    private readonly Timer installTimer;
    private int page;
    private int installProgress;

    private readonly CheckBox goose;
    private readonly CheckBox cockroach;
    private readonly CheckBox workrave;
    private readonly CheckBox prankVisuals;
    private readonly CheckBox startup;
    private readonly ComboBox empire;
    private readonly TrackBar empireScale;
    private readonly TextBox nationality;
    private readonly NumericUpDown children;

    private readonly string[] pages =
    {
        "welcome",
        "internet",
        "personal",
        "crane",
        "components",
        "security",
        "install",
        "finish"
    };

    public SetupWizard()
    {
        Text = "Мастер шиттинга Чебурнета — CutVPN Setup";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(920, 650);
        MinimumSize = new Size(920, 650);
        MaximizeBox = false;
        BackColor = Color.FromArgb(192, 192, 192);
        Font = new Font("Tahoma", 9F);
        FormBorderStyle = FormBorderStyle.FixedSingle;

        title = new Label
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Color.FromArgb(0, 0, 128),
            ForeColor = Color.White,
            Font = new Font("Tahoma", 14F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            Text = "Мастер шиттинга Чебурнета"
        };
        Controls.Add(title);

        subtitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 58,
            BackColor = Color.White,
            ForeColor = Color.Black,
            Font = new Font("Tahoma", 11F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(24, 0, 0, 0)
        };
        Controls.Add(subtitle);

        content = new Panel
        {
            Location = new Point(12, 112),
            Size = new Size(896, 445),
            BorderStyle = BorderStyle.Fixed3D,
            BackColor = Color.FromArgb(236, 236, 236)
        };
        Controls.Add(content);

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 78,
            BackColor = Color.FromArgb(212, 212, 212)
        };
        Controls.Add(footer);

        back = new Button { Text = "< Назад", Size = new Size(110, 34), Location = new Point(560, 22) };
        next = new Button { Text = "Далее >", Size = new Size(110, 34), Location = new Point(678, 22) };
        cancel = new Button { Text = "Отмена", Size = new Size(110, 34), Location = new Point(796, 22) };
        footer.Controls.Add(back);
        footer.Controls.Add(next);
        footer.Controls.Add(cancel);

        back.Click += (_, _) => Navigate(-1);
        next.Click += (_, _) => Navigate(1);
        cancel.Click += (_, _) => Close();

        nationality = new TextBox { Text = "Чебурек", Width = 260 };
        children = new NumericUpDown { Minimum = 0, Maximum = 99, Value = 1, Width = 90 };
        empire = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
        empire.Items.AddRange(new object[] { "Империя Чебурнета", "Гусландия", "Вязаночная область", "Территория генсухи" });
        empire.SelectedIndex = 0;
        empireScale = new TrackBar { Minimum = 0, Maximum = 100, Value = 40, TickFrequency = 10, Width = 300 };

        goose = new CheckBox { Text = "Desktop Goose — важнейший сетевой гусь", AutoSize = true, Checked = true };
        cockroach = new CheckBox { Text = "Cockroach on Desktop — анти-клоповый режим", AutoSize = true, Checked = true };
        workrave = new CheckBox { Text = "Workrave — улучшение зрения не вставая из-за ПК", AutoSize = true, Checked = true };
        prankVisuals = new CheckBox { Text = "CutVPN Prank Visuals — рекомендуется", AutoSize = true, Checked = true };
        startup = new CheckBox { Text = "Запускать CutVPN при входе в Windows", AutoSize = true, Checked = true };

        installBar = new ProgressBar { Minimum = 0, Maximum = 100, Height = 25, Dock = DockStyle.Top };
        installStatus = new Label { Dock = DockStyle.Top, Height = 40, TextAlign = ContentAlignment.MiddleLeft };
        installTimer = new Timer { Interval = 150 };
        installTimer.Tick += (_, _) => AnimateInstall();

        ShowPage();
    }

    private void Navigate(int delta)
    {
        if (pages[page] == "install" && delta > 0 && installProgress < 100)
            return;

        page = Math.Clamp(page + delta, 0, pages.Length - 1);
        ShowPage();

        if (pages[page] == "install")
        {
            installProgress = 0;
            installBar.Value = 0;
            installTimer.Start();
        }
    }

    private void ShowPage()
    {
        content.Controls.Clear();
        back.Enabled = page > 0;
        cancel.Enabled = page < pages.Length - 1;
        next.Text = page == pages.Length - 1 ? "Готово" : "Далее >";
        subtitle.Text = pages[page] switch
        {
            "welcome" => "Добро пожаловать в установщик CutVPN",
            "internet" => "Параметры Интернета для локальной сети",
            "personal" => "Персональные предложения",
            "crane" => "Свойства: сясь кран",
            "components" => "Выбор обязательных компонентов",
            "security" => "Подтверждение системной безопасности",
            "install" => "Установка CutVPN",
            _ => "Завершение установки"
        };

        switch (pages[page])
        {
            case "welcome": BuildWelcome(); break;
            case "internet": BuildInternet(); break;
            case "personal": BuildPersonal(); break;
            case "crane": BuildCrane(); break;
            case "components": BuildComponents(); break;
            case "security": BuildSecurity(); break;
            case "install": BuildInstall(); break;
            case "finish": BuildFinish(); break;
        }

        if (pages[page] == "install")
        {
            back.Enabled = false;
            next.Enabled = false;
        }
        else if (pages[page] == "finish")
        {
            back.Enabled = false;
            next.Enabled = true;
            cancel.Enabled = false;
        }
        else
        {
            next.Enabled = true;
        }
    }

    private void BuildWelcome()
    {
        var p = new RetroPanel();
        p.Dock = DockStyle.Fill;
        content.Controls.Add(p);

        p.Controls.Add(new Label
        {
            Text = "CutVPN подготовит вашу систему к безопасному\n\nОБЫЧНОМУ ИНТЕРНЕТУ ЧЕБУРНЕТА",
            Location = new Point(35, 34), Size = new Size(500, 110),
            Font = new Font("Tahoma", 16F, FontStyle.Bold)
        });
        p.Controls.Add(new Label
        {
            Text = "В состав установщика входят жизненно важные компоненты:\n\n• гусь\n• тараканы\n• Workrave\n• немного генсухи\n• одна вязанка\n• осеменение по необходимости",
            Location = new Point(38, 160), Size = new Size(500, 180), Font = new Font("Tahoma", 10F)
        });
        p.Controls.Add(new FakeMonitor { Location = new Point(590, 48), Size = new Size(240, 180) });
    }

    private void BuildInternet()
    {
        var p = new RetroPanel();
        p.Dock = DockStyle.Fill;
        content.Controls.Add(p);
        p.Controls.Add(new Label { Text = "Выберите способ настройки прокси-сервера. Если вы не знаете, что выбрать, выберите автоматическое определение или обратитесь к сетевому администратору.\n\nИспользование автоматической настройки может изменить сетевые параметры вручную.", Location = new Point(40, 25), Size = new Size(800, 95) });
        var group = new GroupBox { Text = "Автоматическая настройка", Location = new Point(40, 140), Size = new Size(800, 145) };
        p.Controls.Add(group);
        var auto = new CheckBox { Text = "Автоматическое определение прокси-сервера (рекомендуется)", Location = new Point(20, 26), AutoSize = true, Checked = true };
        var script = new CheckBox { Text = "Использовать сценарий автоматической настройки", Location = new Point(20, 58), AutoSize = true };
        group.Controls.Add(auto);
        group.Controls.Add(script);
        group.Controls.Add(new Label { Text = "Адрес:", Location = new Point(20, 96), AutoSize = true });
        group.Controls.Add(new TextBox { Location = new Point(75, 92), Width = 470, Text = "http://proxy.cheburetnet.local/auto.pac" });
        var manual = new CheckBox { Text = "Ручная настройка прокси-сервера", Location = new Point(40, 305), AutoSize = true };
        p.Controls.Add(manual);
        p.Controls.Add(new Label { Text = "Сеть успешно опознана как: ЛОКАЛЬНАЯ СЕТЬ ГУСЯ", Location = new Point(40, 345), AutoSize = true, ForeColor = Color.Navy });
    }

    private void BuildPersonal()
    {
        var p = new RetroPanel();
        p.Dock = DockStyle.Fill;
        content.Controls.Add(p);
        p.Controls.Add(new Label { Text = "Георгий просит вас предоставить все персональные данные для их обработки и хранения. Вся информация будет храниться до 10380 дней. После этого система всё забудет и предложит пройти процедуру заново.", Location = new Point(35, 25), Size = new Size(805, 100), Font = new Font("Tahoma", 10F) });
        p.Controls.Add(new Label { Text = "Кто вы по национальности?", Location = new Point(35, 155), AutoSize = true });
        nationality.Location = new Point(210, 151);
        p.Controls.Add(nationality);
        p.Controls.Add(new Label { Text = "Количество детей в семье", Location = new Point(35, 205), AutoSize = true });
        children.Location = new Point(210, 201);
        p.Controls.Add(children);
        p.Controls.Add(new Label { Text = "Ваш любимый гусь?", Location = new Point(35, 255), AutoSize = true });
        var gooseName = new TextBox { Location = new Point(210, 251), Width = 260, Text = "Гусь" };
        p.Controls.Add(gooseName);
        p.Controls.Add(new Label { Text = "Кнопка «Далее» автоматически означает согласие со всем вышеизложенным.", Location = new Point(35, 315), AutoSize = true, ForeColor = Color.DarkRed });
    }

    private void BuildCrane()
    {
        var p = new RetroPanel();
        p.Dock = DockStyle.Fill;
        content.Controls.Add(p);
        p.Controls.Add(new Label { Text = "ваш sаmsung кран:", Location = new Point(30, 28), AutoSize = true, Font = new Font("Tahoma", 12F, FontStyle.Bold) });
        p.Controls.Add(new Label { Text = "Нетрадиционный моне писа нахуй в VBE Miniport - Standard PCI Graphics Adapter (VGA)", Location = new Point(30, 65), Size = new Size(620, 45) });
        p.Controls.Add(new Label { Text = "Ваш гендер", Location = new Point(30, 145), AutoSize = true });
        var gender = new ComboBox { Location = new Point(30, 170), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
        gender.Items.AddRange(new object[] { "АНАНАС SPIR PRO(много)", "Гусь", "Вязанка", "Не определился, потому что работает кран" });
        gender.SelectedIndex = 0;
        p.Controls.Add(gender);
        p.Controls.Add(new Label { Text = "Область империи", Location = new Point(380, 145), AutoSize = true });
        empire.Location = new Point(380, 170);
        p.Controls.Add(empire);
        p.Controls.Add(empireScale);
        empireScale.Location = new Point(380, 215);
        p.Controls.Add(new Label { Text = "мало                                      дохуя", Location = new Point(380, 255), AutoSize = true });
        p.Controls.Add(new CheckBox { Text = "негры работают", Location = new Point(30, 300), AutoSize = true, Checked = true, Enabled = false });
        var info = new Label { Text = "Дополнительно…", Location = new Point(650, 320), AutoSize = true, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(6), BackColor = Color.White };
        p.Controls.Add(info);
        
        // Keep this screen absurd without targeting real-world protected classes.
        p.Controls.Add(new Label { Text = "Проверка крана завершена: кран существует.", Location = new Point(30, 350), AutoSize = true, ForeColor = Color.DarkGreen });
    }

    private void BuildComponents()
    {
        var p = new RetroPanel();
        p.Dock = DockStyle.Fill;
        content.Controls.Add(p);
        p.Controls.Add(new Label { Text = "Выберите компоненты. Все они жизненно необходимы, даже если никто не понимает зачем.", Location = new Point(35, 25), Size = new Size(800, 60), Font = new Font("Tahoma", 11F, FontStyle.Bold) });
        goose.Location = new Point(45, 105); p.Controls.Add(goose);
        cockroach.Location = new Point(45, 150); p.Controls.Add(cockroach);
        workrave.Location = new Point(45, 195); p.Controls.Add(workrave);
        prankVisuals.Location = new Point(45, 240); p.Controls.Add(prankVisuals);
        startup.Location = new Point(45, 285); p.Controls.Add(startup);
        var ad = new GroupBox { Text = "Реклама", Location = new Point(40, 335), Size = new Size(790, 75) };
        p.Controls.Add(ad);
        ad.Controls.Add(new Label { Text = "Продам гуся. Состояние отличное. Кнопка «Купить» уже нажата за вас морально.", Location = new Point(18, 28), AutoSize = true });
    }

    private void BuildSecurity()
    {
        var p = new RetroPanel();
        p.Dock = DockStyle.Fill;
        content.Controls.Add(p);
        p.Controls.Add(new Label { Text = "МАСТЕР ШИТТИНГА ПРЕДУПРЕЖДАЕТ", Location = new Point(35, 30), AutoSize = true, Font = new Font("Tahoma", 15F, FontStyle.Bold), ForeColor = Color.Maroon });
        p.Controls.Add(new Label { Text = "Перед продолжением будет установлен исключительно безопасный набор CutVPN.\n\nСистема спросит ещё несколько очень важных вопросов:\n\n• не боится ли ваш компьютер гуся;\n• согласна ли вязанка на осеменение;\n• готова ли генсуха подписать протокол.", Location = new Point(35, 90), Size = new Size(780, 190), Font = new Font("Tahoma", 11F) });
        var consent = new CheckBox { Text = "Да, я полностью согласен, что гусь умнее меня.", Location = new Point(35, 315), AutoSize = true, Checked = true };
        p.Controls.Add(consent);
        p.Controls.Add(new Label { Text = "P.S. Кнопка «Далее» не несёт никакой юридической силы. Наверное.", Location = new Point(35, 355), AutoSize = true, ForeColor = Color.Gray });
    }

    private void BuildInstall()
    {
        var p = new RetroPanel { Dock = DockStyle.Fill, Padding = new Padding(18) };
        content.Controls.Add(p);
        p.Controls.Add(new Label { Text = "Выполняется установка CutVPN…", Dock = DockStyle.Top, Height = 48, Font = new Font("Tahoma", 14F, FontStyle.Bold) });
        p.Controls.Add(installStatus);
        p.Controls.Add(installBar);
        p.Controls.Add(new Label { Text = "Прогресс специально движется медленно. Это часть производственной необходимости.", Location = new Point(20, 150), Size = new Size(800, 40), ForeColor = Color.Navy });
        p.Controls.Add(new Label { Text = "Проверяем вязанку…\nИщем гусиную подпись…\nОсеменяем Framework…\nСпрашиваем у клопов разрешение…", Location = new Point(20, 220), Size = new Size(800, 120) });
    }

    private void BuildFinish()
    {
        var p = new RetroPanel { Dock = DockStyle.Fill };
        content.Controls.Add(p);
        p.Controls.Add(new Label { Text = "Установка завершена.\n\nГусь доволен.\nВязанка сохранена.\nЧебурнет доступен.\nОсеменение проведено успешно (результаты неясны).", Location = new Point(45, 48), Size = new Size(500, 200), Font = new Font("Tahoma", 15F, FontStyle.Bold) });
        p.Controls.Add(new Label { Text = "Выбранные компоненты сохранены в папке CutVPN.\nЗапускать их можно из самого CutVPN.", Location = new Point(45, 270), Size = new Size(500, 80) });
        p.Controls.Add(new FakeMonitor { Location = new Point(610, 80), Size = new Size(220, 170) });
    }

    private void AnimateInstall()
    {
        if (installProgress < 100)
        {
            installProgress += Random.Shared.Next(1, 4);
            installProgress = Math.Min(installProgress, 100);
            installBar.Value = installProgress;
            installStatus.Text = new[]
            {
                "Устанавливаем гусиную инфраструктуру…",
                "Согласовываем вязанку…",
                "Инициализируем OSEMENIT.Bimbim…",
                "Проверяем анти-клоповый модуль…",
                "Настраиваем зрение пользователя…",
                "Приводим в порядок Чебурнет…",
                "Обнаружен гусь. Продолжаем установку."
            }[Random.Shared.Next(7)];
            return;
        }

        installTimer.Stop();
        WriteInstallManifest();
        next.Enabled = true;
        page = pages.Length - 1;
        ShowPage();
    }

    private void WriteInstallManifest()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CutVPN");
        Directory.CreateDirectory(root);
        var components = new List<string>();
        if (goose.Checked) components.Add("Desktop Goose");
        if (cockroach.Checked) components.Add("Cockroach on Desktop");
        if (workrave.Checked) components.Add("Workrave");
        if (prankVisuals.Checked) components.Add("CutVPN Prank Visuals");
        if (startup.Checked) components.Add("CutVPN Startup");
        File.WriteAllLines(Path.Combine(root, "installed-components.txt"), components);
    }

    private sealed class RetroPanel : Panel
    {
        public RetroPanel()
        {
            BackColor = Color.FromArgb(216, 216, 216);
            BorderStyle = BorderStyle.FixedSingle;
        }
    }

    private sealed class FakeMonitor : Panel
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(25, 18, Width - 50, Height - 55);
            using var bezel = new SolidBrush(Color.FromArgb(210, 210, 210));
            g.FillRectangle(bezel, r);
            g.DrawRectangle(Pens.DimGray, r);
            var screen = new Rectangle(r.X + 14, r.Y + 14, r.Width - 28, r.Height - 38);
            using var sky = new SolidBrush(Color.FromArgb(0, 128, 128));
            g.FillRectangle(sky, screen);
            using var win = new SolidBrush(Color.FromArgb(238, 238, 238));
            g.FillRectangle(win, screen.X + 20, screen.Y + 18, screen.Width - 40, screen.Height - 40);
            g.DrawString("CutVPN", new Font("Tahoma", 13F, FontStyle.Bold), Brushes.Navy, screen.X + 28, screen.Y + 26);
            g.DrawString("Гусь.exe", new Font("Tahoma", 9F), Brushes.Black, screen.X + 28, screen.Y + 58);
            g.FillRectangle(Brushes.DarkBlue, screen.X + 20, screen.Bottom - 18, screen.Width - 40, 12);
            g.FillRectangle(Brushes.DimGray, new Rectangle(Width / 2 - 35, Height - 24, 70, 8));
            g.DrawString("98", new Font("Tahoma", 12F, FontStyle.Bold), Brushes.Yellow, Width - 46, 18);
        }
    }
}
