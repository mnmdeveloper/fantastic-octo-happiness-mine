using System;
using System.Drawing;
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

    public MainForm()
    {
        Text = "CutVPN — Connection Manager";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(620, 410);
        MinimumSize = new Size(620, 410);
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
            Size = new Size(580, 250),
            BorderStyle = BorderStyle.Fixed3D,
            BackColor = Color.White
        };
        Controls.Add(panel);

        var server = new Label
        {
            Text = "VPN Server:",
            Location = new Point(24, 28),
            AutoSize = true
        };
        panel.Controls.Add(server);

        var combo = new ComboBox
        {
            Location = new Point(105, 24),
            Size = new Size(300, 24),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        combo.Items.AddRange(new object[] { "CutVPN Europe", "CutVPN Home", "Automatic" });
        combo.SelectedIndex = 0;
        panel.Controls.Add(combo);

        var connect = new Button
        {
            Text = "Connect",
            Location = new Point(425, 22),
            Size = new Size(115, 30),
            FlatStyle = FlatStyle.Standard
        };
        panel.Controls.Add(connect);

        status = new Label
        {
            Text = "Status: Disconnected",
            Location = new Point(24, 78),
            AutoSize = true,
            Font = new Font("Tahoma", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(128, 0, 0)
        };
        panel.Controls.Add(status);

        var info = new Label
        {
            Text = "Connection information\n\nServer       CutVPN Europe\nProtocol     Automatic\nIP address   Not connected",
            Location = new Point(24, 125),
            AutoSize = true
        };
        panel.Controls.Add(info);

        connect.Click += (_, _) =>
        {
            status.Text = "Status: Connected";
            status.ForeColor = Color.FromArgb(0, 128, 0);
            connect.Text = "Disconnect";
            connect.Click -= null;
        };

        var footer = new Label
        {
            Text = "CutVPN 1.0  •  Windows XP style interface",
            Location = new Point(22, 355),
            AutoSize = true,
            ForeColor = Color.Gray
        };
        Controls.Add(footer);
    }
}
