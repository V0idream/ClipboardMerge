namespace ClipboardMerge;

internal sealed class SettingsForm : Form
{
    private readonly CheckBox _showNotification;
    private readonly NumericUpDown _duration;
    private readonly CheckBox _sound;
    private readonly CheckBox _autoStart;
    private readonly ComboBox _separator;

    public AppSettings Result { get; private set; }

    public SettingsForm(AppSettings current)
    {
        Result = current;

        Text = "ClipboardMerge 设置";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(440, 300);
        Font = new Font("Microsoft YaHei UI", 9f);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 2,
            RowCount = 7
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));

        _showNotification = new CheckBox
        {
            Text = "复制后显示内容提示",
            Checked = current.ShowCopyNotification,
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };

        _duration = new NumericUpDown
        {
            Minimum = 400,
            Maximum = 5000,
            Increment = 100,
            Value = Math.Clamp(current.NotificationMilliseconds, 400, 5000),
            Width = 110
        };

        _sound = new CheckBox
        {
            Text = "播放提示音",
            Checked = current.PlaySound,
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };

        _autoStart = new CheckBox
        {
            Text = "随 Windows 登录自动启动",
            Checked = current.AutoStart,
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };

        _separator = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 180
        };
        _separator.Items.AddRange(["换行", "空行", "空格", "Tab"]);
        _separator.SelectedIndex = current.Separator switch
        {
            "\r\n\r\n" => 1,
            " " => 2,
            "\t" => 3,
            _ => 0
        };

        layout.Controls.Add(_showNotification, 0, 0);
        layout.SetColumnSpan(_showNotification, 2);
        layout.Controls.Add(new Label { Text = "提示持续时间", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        layout.Controls.Add(_duration, 1, 1);
        layout.Controls.Add(_sound, 0, 2);
        layout.SetColumnSpan(_sound, 2);
        layout.Controls.Add(new Label { Text = "累积内容分隔方式", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
        layout.Controls.Add(_separator, 1, 3);
        layout.Controls.Add(_autoStart, 0, 4);
        layout.SetColumnSpan(_autoStart, 2);

        var hotkeyHelp = new Label
        {
            Text = "快捷键：Win + Shift + C 开始/结束累积；Win + Shift + X 清空当前累计内容。",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Anchor = AnchorStyles.Left
        };
        layout.Controls.Add(hotkeyHelp, 0, 5);
        layout.SetColumnSpan(hotkeyHelp, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };
        var save = new Button { Text = "保存", DialogResult = DialogResult.OK, Width = 90 };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 90 };
        save.Click += (_, _) => SaveResult();
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons, 0, 6);
        layout.SetColumnSpan(buttons, 2);

        Controls.Add(layout);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private void SaveResult()
    {
        Result.ShowCopyNotification = _showNotification.Checked;
        Result.NotificationMilliseconds = (int)_duration.Value;
        Result.PlaySound = _sound.Checked;
        Result.AutoStart = _autoStart.Checked;
        Result.Separator = _separator.SelectedIndex switch
        {
            1 => "\r\n\r\n",
            2 => " ",
            3 => "\t",
            _ => "\r\n"
        };
    }
}
