using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CutVPN;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

public sealed class MainForm : Form
{
    private readonly Label status;
    private readonly NotifyIcon tray;
    private readonly CheckBox prankMode;
    private bool connected;

    public MainForm()
    {
        Text = "CutVPN — Connection Manager";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(700, 470);
        MinimumSize = new Size(700, 470);
        BackColor = Color.FromArgb(236, 233, 216);
        Font = new Font("Tahoma", 9F);

        var title = new Label
        {
            Text = "CutVPN",
            Font = new Font("Tahoma", 18F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 51, 153),
            Location = new Point(22, 18),
            AutoSize = true
        };
        Controls.Add(title);

        var subtitle = new Label
        {
            Text = "Virtual Private Network Connection",
            Location = new Point(24, 50),
            AutoSize = true,
            ForeColor = Color.FromArgb(80, 80, 80)
        };
        Controls.Add(subtitle);

        var panel = new Panel
        {
            Location = new Point(20, 82),
            Size = new Size(660, 300),
            BorderStyle = BorderStyle.Fixed3D,
            BackColor = Color.White
        };
        Controls.Add(panel);

        var server = new Label
        {
            Text = "VPN Server:", Location = new Point(24, 28), AutoSize = true
        };
        panel.Controls.Add(server);

        var combo = new ComboBox
        {
            Location = new Point(105, 24), Size = new Size(300, 24),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        combo.Items.AddRange(new object[] { "CutVPN Europe", "CutVPN Home", "Automatic" });
        combo.SelectedIndex = 0;
        panel.Controls.Add(combo);

        var connect = new Button
        {
            Text = "Connect", Location = new Point(425, 22), Size = new Size(115, 30)
        };
        panel.Controls.Add(connect);

        status = new Label
        {
            Text = "Status: Disconnected", Location = new Point(24, 78), AutoSize = true,
            Font = new Font("Tahoma", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(128, 0, 0)
        };
        panel.Controls.Add(status);

        var info = new Label
        {
            Text = "Connection information\n\nServer       CutVPN Europe\nProtocol     Automatic\nIP address   Demo / not connected",
            Location = new Point(24, 125), AutoSize = true
        };
        panel.Controls.Add(info);

        prankMode = new CheckBox
        {
            Text = "Enable CutVPN prank visuals on startup",
            Location = new Point(24, 220), AutoSize = true,
            Checked = LoadPrankState()
        };
        panel.Controls.Add(prankMode);
        prankMode.CheckedChanged += (_, _) => SavePrankState(prankMode.Checked);

        var hint = new Label
        {
            Text = "Tray icon: right-click for prank controls and emergency stop.",
            Location = new Point(24, 255), AutoSize = true, ForeColor = Color.DimGray
        };
        panel.Controls.Add(hint);

        connect.Click += (_, _) =>
        {
            connected = !connected;
            status.Text = connected ? "Status: Connected" : "Status: Disconnected";
            status.ForeColor = connected ? Color.FromArgb(0, 128, 0) : Color.FromArgb(128, 0, 0);
            connect.Text = connected ? "Disconnect" : "Connect";
        };

        var footer = new Label
        {
            Text = "CutVPN 1.0 • Windows XP / Vista inspired interface • Prank Edition",
            Location = new Point(22, 405), AutoSize = true, ForeColor = Color.Gray
        };
        Controls.Add(footer);

        tray = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "CutVPN",
            Visible = true,
            ContextMenuStrip = BuildTrayMenu()
        };
        tray.DoubleClick += (_, _) => ShowMain();

        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                tray.ShowBalloonTip(1200, "CutVPN", "CutVPN is still running in the tray.", ToolTipIcon.Info);
            }
        };

        FormClosing += (_, _) => tray.Dispose();

        if (prankMode.Checked)
            BeginInvoke(new Action(() => ShowPrankWizard()));
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open CutVPN", null, (_, _) => ShowMain());
        menu.Items.Add("Prank: ON", null, (_, _) => SetPrank(true));
        menu.Items.Add("Prank: OFF", null, (_, _) => SetPrank(false));
        menu.Items.Add("Emergency Stop (Ctrl+Shift+G)", null, (_, _) => EmergencyStop());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Run fake security update", null, (_, _) => ShowSecurityUpdate());
        menu.Items.Add("Run random nonsense error", null, (_, _) => ShowRandomError());
        menu.Items.Add("Exit CutVPN", null, (_, _) => Application.Exit());
        return menu;
    }

    private void SetPrank(bool enabled)
    {
        prankMode.Checked = enabled;
        if (enabled) ShowPrankWizard();
        else tray.ShowBalloonTip(900, "CutVPN", "Prank visuals disabled.", ToolTipIcon.Info);
    }

    private void EmergencyStop()
    {
        prankMode.Checked = false;
        tray.ShowBalloonTip(1200, "CutVPN", "Emergency stop activated. Visuals disabled.", ToolTipIcon.Warning);
    }

    private void ShowMain()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ShowPrankWizard()
    {
        using var dialog = new PrankWizard();
        dialog.ShowDialog(this);
    }

    private void ShowSecurityUpdate()
    {
        using var dialog = new PrankWizard("Обновление системы безопасности");
        dialog.ShowDialog(this);
    }

    private void ShowRandomError()
    {
        var rnd = new Random();
        MessageBox.Show(PrankContent.RandomErrors[rnd.Next(PrankContent.RandomErrors.Length)],
            "CutVPN — Критическая хуета", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private static string StatePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CutVPN", "prank.state");

    private static bool LoadPrankState()
        => File.Exists(StatePath) && File.ReadAllText(StatePath).Trim() == "on";

    private static void SavePrankState(bool enabled)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
        File.WriteAllText(StatePath, enabled ? "on" : "off");
    }

    private sealed class PrankWizard : Form
    {
        private readonly Label body;
        private readonly ProgressBar bar;
        private readonly System.Windows.Forms.Timer timer;
        private int step;
        private readonly string[] pages;

        public PrankWizard(string? title = null)
        {
            Text = title ?? "CutVPN — Мастер шиттинга Чебурнета";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(820, 560);
            BackColor = Color.FromArgb(192, 192, 192);
            Font = new Font("Tahoma", 10F);
            KeyPreview = true;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;

            pages = PrankContent.InstallationPages;
            body = new Label { Text = pages[0], Location = new Point(30, 35), Size = new Size(760, 310), Font = new Font("Tahoma", 12F) };
            Controls.Add(body);

            bar = new ProgressBar { Location = new Point(30, 390), Size = new Size(760, 24), Value = 10 };
            Controls.Add(bar);

            var next = new Button { Text = "Далее >", Location = new Point(670, 455), Size = new Size(120, 35) };
            next.Click += (_, _) => Next(next);
            Controls.Add(next);

            var cancel = new Button { Text = "Отмена", Location = new Point(530, 455), Size = new Size(120, 35) };
            cancel.Click += (_, _) => Close();
            Controls.Add(cancel);

            var safe = new Label
            {
                Text = "CutVPN prank mode • Esc закрывает это окно • Ctrl+Shift+G выключает визуалы",
                Location = new Point(30, 510), AutoSize = true, ForeColor = Color.DimGray
            };
            Controls.Add(safe);

            timer = new System.Windows.Forms.Timer { Interval = 4200 };
            timer.Tick += (_, _) =>
            {
                if (Random.Shared.NextDouble() < 0.45)
                {
                    var m = PrankContent.RandomErrors[Random.Shared.Next(PrankContent.RandomErrors.Length)];
                    MessageBox.Show(m, "CutVPN — Диагностика", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            timer.Start();

            KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        }

        private void Next(Button next)
        {
            step++;
            if (step >= pages.Length)
            {
                timer.Stop();
                body.Text = "Готово!\n\nCutVPN установлен. Гусь доволен. Вязанка сохранена.\n\nКомпоненты можно отключить из меню в трее или удалить обычным деинсталлятором.";
                bar.Value = 100;
                next.Enabled = false;
                return;
            }
            body.Text = pages[step];
            bar.Value = Math.Min(100, 10 + step * 14);
        }
    }
}
