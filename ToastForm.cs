using System.Drawing.Drawing2D;
using System.Media;

namespace ClipboardMerge;

internal sealed class ToastForm : Form
{
    private readonly Label _titleLabel;
    private readonly Label _bodyLabel;
    private readonly System.Windows.Forms.Timer _timer;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int WS_EX_NOACTIVATE = 0x08000000;
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    public ToastForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(36, 36, 36);
        ForeColor = Color.White;
        Width = 420;
        Height = 112;
        Padding = new Padding(16, 12, 16, 12);

        _titleLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _bodyLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.Gainsboro,
            TextAlign = ContentAlignment.TopLeft,
            AutoEllipsis = true
        };

        Controls.Add(_bodyLabel);
        Controls.Add(_titleLabel);

        _timer = new System.Windows.Forms.Timer();
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            Hide();
        };

        Resize += (_, _) => ApplyRoundedRegion();
        ApplyRoundedRegion();
    }

    public void ShowMessage(string title, string body, int durationMs, bool playSound)
    {
        _titleLabel.Text = title;
        _bodyLabel.Text = body;

        var area = Screen.PrimaryScreen?.WorkingArea ?? Screen.GetWorkingArea(Cursor.Position);
        var bodyWidth = Width - Padding.Horizontal;
        var measured = TextRenderer.MeasureText(
            body,
            _bodyLabel.Font,
            new Size(bodyWidth, 10000),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
        Height = Math.Clamp(28 + measured.Height + Padding.Vertical + 6, 88, Math.Min(360, area.Height - 36));
        var x = area.Left + (area.Width - Width) / 2;
        var y = area.Bottom - Height - 32;
        Location = new Point(x, y);

        _timer.Stop();
        _timer.Interval = Math.Clamp(durationMs, 400, 5000);
        _timer.Start();

        if (playSound)
            SystemSounds.Asterisk.Play();

        if (!Visible)
            Show();
        else
            Invalidate();
    }

    private void ApplyRoundedRegion()
    {
        const int radius = 18;
        using var path = new GraphicsPath();
        path.AddArc(0, 0, radius, radius, 180, 90);
        path.AddArc(Width - radius, 0, radius, radius, 270, 90);
        path.AddArc(Width - radius, Height - radius, radius, radius, 0, 90);
        path.AddArc(0, Height - radius, radius, radius, 90, 90);
        path.CloseFigure();
        Region = new Region(path);
    }
}
