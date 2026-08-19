using System;
using System.Drawing;
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
    private readonly Label heading;
    private readonly Label body;
    private readonly ProgressBar progress;
    private readonly Label status;
    private readonly Button back;
    private readonly Button next;
    private readonly Button cancel;
    private readonly System.Windows.Forms.Timer timer;
    private readonly CheckBox goose;
    private readonly CheckBox cockroach;
    private readonly CheckBox workrave;
    private readonly CheckBox prank;
    private readonly CheckBox startup;
    private readonly TextBox nationality;
    private readonly NumericUpDown children;
    private readonly ComboBox empire;
    private int page;
    private int ticks;

    private static readonly string[] Titles =
    {
        "Добро пожаловать в CutVPN",
        "Параметры Интернета для локальной сети",
        "Персональные предложения",
        "Свойства: сясь кран",
        "Выбор обязательных компонентов",
        "Подтверждение системной безопасности",
        "Установка CutVPN",
        "Установка завершена"
    };

    public SetupWizard()
    {
        Text = "Мастер шиттинга Чебурнета — CutVPN Setup";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(920, 650);
        MinimumSize = ClientSize;
        BackColor = Color.FromArgb(192, 192, 192);
        Font = new Font("Tahoma", 9F);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        var top = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.Navy };
        Controls.Add(top);
        var topText = new Label
        {
            Text = "Мастер шиттинга Чебурнета",
            ForeColor = Color.White,
            Font = new Font("Tahoma", 14F, FontStyle.Bold),
            Location = new Point(14, 11),
            AutoSize = true
        };
        top.Controls.Add(topText);

        heading = new Label
        {
            Location = new Point(22, 64),
            Size = new Size(850, 30),
            Font = new Font("Tahoma", 13F, FontStyle.Bold),
            ForeColor = Color.Black
        };
        Controls.Add(heading);

        var page = new Panel
        {
            Name = "Page",
            Location = new Point(18, 102),
            Size = new Size(884, 430),
            BorderStyle = BorderStyle.Fixed3D,
            BackColor = Color.FromArgb(236, 236, 236)
        };
        Controls.Add(page);

        body = new Label
        {
            Location = new Point(24, 24),
            Size = new Size(825, 250),
            Font = new Font("Tahoma", 11F)
        };
        page.Controls.Add(body);

        progress = new ProgressBar
        {
            Location = new Point(24, 300),
            Size = new Size(825, 25),
            Minimum = 0,
            Maximum = 100,
            Value = 0
        };
        page.Controls.Add(progress);

        status = new Label
        {
            Location = new Point(24, 333),
            Size = new Size(825, 35)
        };
        page.Controls.Add(status);

        goose = new CheckBox { Text = "Desktop Goose — важнейший сетевой гусь", AutoSize = true, Checked = true, Location = new Point(40, 24) };
        cockroach = new CheckBox { Text = "Cockroach on Desktop — анти-клоповый режим", AutoSize = true, Checked = true, Location = new Point(40, 66) };
        workrave = new CheckBox { Text = "Workrave — улучшение зрения не вставая из-за ПК", AutoSize = true, Checked = true, Location = new Point(40, 108) };
        prank = new CheckBox { Text = "CutVPN Prank Visuals — рекомендуется", AutoSize = true, Checked = true, Location = new Point(40, 150) };
        startup = new CheckBox { Text = "Запускать CutVPN при входе в Windows", AutoSize = true, Checked = true, Location = new Point(40, 192) };

        nationality = new TextBox { Text = "Чебурек", Location = new Point(240, 150), Width = 260 };
        children = new NumericUpDown { Minimum = 0, Maximum = 99, Value = 1, Location = new Point(240, 198), Width = 90 };
        empire = new ComboBox { Location = new Point(240, 246), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
        empire.Items.AddRange(new object[] { "Империя Чебурнета", "Гусландия", "Вязаночная область", "Территория генсухи" });
        empire.SelectedIndex = 0;

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 72, BackColor = Color.FromArgb(212, 212, 212) };
        Controls.Add(bottom);
        back = new Button { Text = "< Назад", Size = new Size(110, 34), Location = new Point(548, 18) };
        next = new Button { Text = "Далее >", Size = new Size(110, 34), Location = new Point(665, 18) };
        cancel = new Button { Text = "Отмена", Size = new Size(110, 34), Location = new Point(782, 18) };
        bottom.Controls.Add(back);
        bottom.Controls.Add(next);
        bottom.Controls.Add(cancel);

        back.Click += (_, _) => Navigate(-1, page);
        next.Click += (_, _) => Navigate(1, page);
        cancel.Click += (_, _) => Close();

        timer = new System.Windows.Forms.Timer { Interval = 250 };
        timer.Tick += (_, _) => TickInstall();

        ShowPage(page);
    }

    private void Navigate(int delta, Panel pageHost)
    {
        if (page == 6 && delta > 0 && progress.Value < 100) return;
        page = Math.Clamp(page + delta, 0, Titles.Length - 1);
        if (page == 6)
        {
            progress.Value = 0;
            ticks = 0;
            timer.Start();
        }
        else
        {
            timer.Stop();
        }
        ShowPage(pageHost);
    }

    private void ShowPage(Panel host)
    {
        heading.Text = Titles[page];
        host.Controls.Clear();
        host.Controls.Add(body);
        host.Controls.Add(progress);
        host.Controls.Add(status);

        back.Enabled = page > 0 && page != 6;
        cancel.Enabled = page < Titles.Length - 1;
        next.Enabled = page != 6;
        next.Text = page == Titles.Length - 1 ? "Готово" : "Далее >";

        progress.Visible = page == 6;
        status.Visible = page == 6;

        if (page == 0)
        {
            body.Text = "CutVPN подготовит вашу систему к безопасному\n\nОБЫЧНОМУ ИНТЕРНЕТУ ЧЕБУРНЕТА.\n\nВ установщике есть гусь, тараканы, Workrave, генсуха, вязанка и осеменение по необходимости.\n\nНажмите «Далее», чтобы начать абсолютно серьёзную процедуру.";
        }
        else if (page == 1)
        {
            body.Text = "Выберите способ настройки прокси-сервера.\n\n☑ Автоматическое определение прокси-сервера (рекомендуется)\n☐ Использовать сценарий автоматической настройки\n\nАдрес: http://proxy.cheburetnet.local/auto.pac\n\n☐ Ручная настройка прокси-сервера\n\nСеть успешно опознана как: ЛОКАЛЬНАЯ СЕТЬ ГУСЯ";
        }
        else if (page == 2)
        {
            body.Text = "Георгий просит вас предоставить все персональные данные для их обработки и хранения. Вся информация будет храниться до 10380 дней.\n\nКто вы по национальности?";
            host.Controls.Add(nationality);
            host.Controls.Add(new Label { Text = "Количество детей в семье", Location = new Point(40, 198), AutoSize = true });
            host.Controls.Add(children);
            host.Controls.Add(new Label { Text = "Ваш любимый гусь?", Location = new Point(40, 246), AutoSize = true });
            host.Controls.Add(new TextBox { Text = "Гусь", Location = new Point(240, 246), Width = 260 });
            host.Controls.Add(new Label { Text = "Кнопка «Далее» автоматически означает согласие со всем вышеизложенным.", Location = new Point(40, 300), AutoSize = true, ForeColor = Color.Maroon });
        }
        else if (page == 3)
        {
            body.Text = "ваш sаmsung кран:\n\nНетрадиционный моне писа нахуй в VBE Miniport - Standard PCI Graphics Adapter (VGA).\n\nВаш гендер: АНАНАС SPIR PRO(много)\n\nОбласть империи:";
            host.Controls.Add(empire);
            host.Controls.Add(new TrackBar { Location = new Point(40, 310), Width = 300, Minimum = 0, Maximum = 100, Value = 40 });
            host.Controls.Add(new Label { Text = "мало                                      дохуя", Location = new Point(40, 350), AutoSize = true });
            host.Controls.Add(new Label { Text = "Проверка крана завершена: кран существует.", Location = new Point(40, 382), AutoSize = true, ForeColor = Color.DarkGreen });
        }
        else if (page == 4)
        {
            body.Text = "Выберите компоненты. Все они жизненно необходимы, даже если никто не понимает зачем.\n\n";
            host.Controls.Add(goose);
            host.Controls.Add(cockroach);
            host.Controls.Add(workrave);
            host.Controls.Add(prank);
            host.Controls.Add(startup);
            host.Controls.Add(new Label { Text = "Реклама: улучшение зрения, уничтожение клопов и продажа гуся — всё в одном тарифе.", Location = new Point(40, 250), AutoSize = true, ForeColor = Color.Navy });
        }
        else if (page == 5)
        {
            body.Text = "Системная безопасность подтверждена.\n\n☑ Обновление Framework по доению коровы\n☑ Проверка OSEMENIT.Bimbim\n☑ Согласование вязанки с генсухой\n☑ Проверка наличия гуся\n\nНажмите «Далее», чтобы начать установку.";
        }
        else if (page == 6)
        {
            body.Text = "Установка CutVPN\n\nПодождите, пока мастер выполняет чрезвычайно важные действия.\n\nГусь: в процессе\nТараканы: почти готовы\nWorkrave: моргает\nЧебурнет: нестабильен";
            status.Text = "Упорядочиваем вязанку...";
        }
        else
        {
            body.Text = "Установка завершена.\n\nCutVPN установлен. Гусь доволен. Вязанка сохранена.\n\nВыбранные компоненты были подготовлены.\n\nНажмите «Готово», чтобы закрыть мастер.";
        }
    }

    private void TickInstall()
    {
        ticks++;
        if (progress.Value < 100) progress.Value = Math.Min(100, progress.Value + (ticks % 7 == 0 ? 3 : 1));
        status.Text = new[]
        {
            "Упорядочиваем вязанку...",
            "Согласовываем с генсухой...",
            "Ищем OSEMENIT.Bimbim...",
            "Проверяем, где гусь...",
            "Ускоряем Чебурнет...",
            "Спрашиваем клопов, всё ли им нравится..."
        }[ticks % 6];
        if (progress.Value >= 100)
        {
            timer.Stop();
            next.Enabled = true;
            cancel.Enabled = false;
        }
    }
}
