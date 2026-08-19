using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CutVPN;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new InstallerForm());
    }
}

public sealed class InstallerForm : Form
{
    private readonly Label body;
    private readonly Label status;
    private readonly ProgressBar progress;
    private readonly Button next;
    private readonly Button cancel;
    private readonly System.Windows.Forms.Timer fakeWork;
    private int page;
    private int ticks;
    private bool finished;

    private static readonly string[] Pages =
    {
        "Добро пожаловать в CutVPN\n\nМастер шиттинга Чебурнета подготовит ваш персональный интернет нового поколения.\n\nПеред началом рекомендуется убрать кружку, кота и все вязанки с системного блока.",
        "Персональные предложения\n\nХотите улучшить зрение, не вставая из-за ПК?\n\nCutVPN совершенно случайно нашёл для вас Workrave.\nПоставим? Конечно поставим. Мы уже почти всё решили.",
        "Спецпредложение для дома\n\nУничтожение клопов из дома!\n\nУникальная технология Cockroach on Desktop определяет клопов по уровню наглости мыши.\n\nПредлагаем установить модуль наблюдения за тараканами.",
        "ЭКСКЛЮЗИВНАЯ ТОРГОВЛЯ\n\nПРОДАМ ГУСЯ.\n\nГусь проверен, вязанка в комплекте, генсуха не возражает.\n\n[ КУПИТЬ ]\n\nДругих кнопок не предусмотрено по техническим причинам.",
        "Мастер шиттинга Чебурнета\n\nАвтоматическая настройка прокси-сервера...\n\nПроверка осеменения сетевого адаптера...\n\nГЕНСУХА согласовывает вязанку...\n\nГусь ожидает подтверждения.",
        "Обновление Framework по доению коровы\n\nКомпоненты: 7\nДойка: 84%\nОсеменение: 97%\nВязанка: критически необходима\nГусь: работает сверхурочно",
        "СИСТЕМНАЯ ПРОВЕРКА\n\nOSEMENIT.Bimbim ............. OK\nGENSUHA.dll .................. OK\nVYAZANKA.sys ................. OK\nGUS.EXE ....................... думает\nCHEBUREK.NET ................. почти готов",
        "Последние новости Чебурнета\n\nГенсуха подписала соглашение о поставке вязанок.\nГусь назначен временным министром осеменения.\nWorkrave настоятельно рекомендует моргать.\nТараканы требуют отдельный канал связи.",
        "ПОЧТИ ГОТОВО\n\nCutVPN устанавливает самые необходимые вещи:\n\n• гусь\n• тараканы\n• отдых для глаз\n• странные новости\n• ошибки, которые никто не просил\n\nОжидаем подтверждение Чебурнета..."
    };

    public InstallerForm()
    {
        Text = "CutVPN — Мастер шиттинга Чебурнета";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        BackColor = Color.FromArgb(192, 192, 192);
        Font = new Font("Tahoma", 10F);
        KeyPreview = true;

        var top = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.FromArgb(0, 0, 128) };
        Controls.Add(top);

        var title = new Label
        {
            Text = "CutVPN — Мастер шиттинга Чебурнета",
            ForeColor = Color.White,
            Font = new Font("Tahoma", 16F, FontStyle.Bold),
            Location = new Point(22, 17),
            AutoSize = true
        };
        top.Controls.Add(title);

        var logo = new Label
        {
            Text = "C",
            ForeColor = Color.FromArgb(0, 0, 128),
            BackColor = Color.White,
            Font = new Font("Tahoma", 22F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(42, 42),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        logo.Location = new Point(ClientSize.Width - 64, 11);
        logo.Parent = top;
        top.Resize += (_, _) => logo.Left = top.ClientSize.Width - logo.Width - 20;

        body = new Label
        {
            Text = Pages[0],
            Location = new Point(70, 120),
            Size = new Size(1000, 420),
            Font = new Font("Tahoma", 18F),
            AutoSize = false
        };
        Controls.Add(body);

        var fakeNews = new Label
        {
            Text = "Срочная новость: вязанка снова была замечена рядом с генсухой.\nГусь это отрицает.",
            Location = new Point(70, 555),
            AutoSize = true,
            Font = new Font("Tahoma", 11F, FontStyle.Italic),
            ForeColor = Color.Navy
        };
        Controls.Add(fakeNews);

        progress = new ProgressBar
        {
            Location = new Point(70, 625),
            Size = new Size(960, 30),
            Minimum = 0,
            Maximum = 100,
            Value = 7,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };
        Controls.Add(progress);

        status = new Label
        {
            Text = "Подготовка к шиттингу...",
            Location = new Point(70, 670),
            AutoSize = true,
            Font = new Font("Tahoma", 10F)
        };
        status.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
        Controls.Add(status);

        next = new Button
        {
            Text = "Далее >",
            Size = new Size(130, 42),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        next.Click += (_, _) => NextPage();
        Controls.Add(next);

        cancel = new Button
        {
            Text = "Отмена",
            Size = new Size(130, 42),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        cancel.Click += (_, _) => Close();
        Controls.Add(cancel);

        var hint = new Label
        {
            Text = "Esc / Win+U — выйти из полноэкранного режима. Ctrl+Shift+G — аварийно остановить визуалы.",
            Location = new Point(70, 720),
            AutoSize = true,
            ForeColor = Color.FromArgb(80, 80, 80),
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom
        };
        Controls.Add(hint);

        Resize += (_, _) => LayoutBottom();
        LayoutBottom();

        fakeWork = new System.Windows.Forms.Timer { Interval = 700 };
        fakeWork.Tick += (_, _) => TickFakeInstall();
        fakeWork.Start();

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
                Close();
            if (e.KeyCode == Keys.U && (e.Modifiers & Keys.LWin) != Keys.None)
                Close();
            if (e.KeyCode == Keys.G && e.Modifiers == (Keys.Control | Keys.Shift))
            {
                SavePrankState(false);
                Close();
            }
        };
    }

    private void LayoutBottom()
    {
        next.Location = new Point(ClientSize.Width - 290, ClientSize.Height - 92);
        cancel.Location = new Point(ClientSize.Width - 145, ClientSize.Height - 92);
        progress.Location = new Point(70, ClientSize.Height - 170);
        progress.Width = Math.Max(400, ClientSize.Width - 140);
        status.Location = new Point(70, ClientSize.Height - 128);
    }

    private void TickFakeInstall()
    {
        if (finished) return;

        ticks++;
        progress.Value = Math.Min(98, progress.Value + (ticks % 3 == 0 ? 2 : 1));
        status.Text = ticks switch
        {
            < 4 => "Проверка гусиной совместимости...",
            < 8 => "Поиск вязанки...",
            < 12 => "Согласование с генсухой...",
            < 16 => "Осеменение Framework...",
            < 20 => "Проверка тараканов на вменяемость...",
            _ => "Установка совершенно необходимых компонентов..."
        };

        if (ticks % 7 == 0 && Random.Shared.NextDouble() < 0.55)
        {
            var msg = PrankContent.RandomErrors[Random.Shared.Next(PrankContent.RandomErrors.Length)];
            MessageBox.Show(msg, "CutVPN — критическая хуета", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void NextPage()
    {
        if (finished) { Close(); return; }

        page++;
        if (page >= Pages.Length)
        {
            finished = true;
            fakeWork.Stop();
            progress.Value = 100;
            status.Text = "Шиттинг успешно завершён.";
            body.Text = "УСТАНОВКА ЗАКОНЧЕНА\n\nCutVPN готов.\n\nГусь продан.\nТараканы проинформированы.\nЗрение улучшено без вставания от ПК.\nВязанка сохранена.\n\nНажмите «Готово», чтобы перейти в CutVPN.";
            next.Text = "Готово";
            return;
        }

        body.Text = Pages[page];
        progress.Value = Math.Min(98, 8 + page * 10);
    }

    private static string StatePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CutVPN", "prank.state");

    private static void SavePrankState(bool enabled)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
        File.WriteAllText(StatePath, enabled ? "on" : "off");
    }
}
