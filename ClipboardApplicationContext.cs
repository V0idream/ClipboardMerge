namespace ClipboardMerge;

internal sealed class ClipboardApplicationContext : ApplicationContext
{
    private const string InternalClipboardFormat = "ClipboardMerge.Internal";
    private const long InactivityTimeoutMilliseconds = 60_000;
    private readonly MessageWindow _window;
    private readonly KeyboardHook _keyboardHook;
    private readonly System.Windows.Forms.Timer _inactivityTimer;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly ToolStripMenuItem _clearItem;
    private readonly ToolStripMenuItem _autoStartItem;
    private readonly ToastForm _toast;
    private readonly List<string> _items = [];

    private AppSettings _settings;
    private bool _collecting;
    private long _lastCollectionActivityTick;

    public ClipboardApplicationContext()
    {
        _settings = AppSettings.Load();
        _settings.AutoStart = StartupManager.IsEnabled() || _settings.AutoStart;
        _settings.Save();
        StartupManager.SetEnabled(_settings.AutoStart);

        _toast = new ToastForm();
        _inactivityTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _inactivityTimer.Tick += (_, _) => CheckCollectionInactivity();
        _window = new MessageWindow();
        _window.ClipboardChanged += (_, _) => HandleClipboardChanged();

        _keyboardHook = new KeyboardHook();
        _keyboardHook.ToggleRequested += (_, _) => ToggleCollection();
        _keyboardHook.ClearRequested += (_, _) => ClearCollection();

        _statusItem = new ToolStripMenuItem("状态：普通模式") { Enabled = false };
        _toggleItem = new ToolStripMenuItem("开始累积复制    Win+Shift+C");
        _toggleItem.Click += (_, _) => ToggleCollection();

        _clearItem = new ToolStripMenuItem("清空当前累积") { Enabled = false };
        _clearItem.Click += (_, _) => ClearCollection();

        var settingsItem = new ToolStripMenuItem("设置...");
        settingsItem.Click += (_, _) => OpenSettings();

        _autoStartItem = new ToolStripMenuItem("开机自启")
        {
            CheckOnClick = true,
            Checked = _settings.AutoStart
        };
        _autoStartItem.CheckedChanged += (_, _) =>
        {
            _settings.AutoStart = _autoStartItem.Checked;
            _settings.Save();
            StartupManager.SetEnabled(_settings.AutoStart);
        };

        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (_, _) => Exit();

        var menu = new ContextMenuStrip();
        menu.Items.AddRange([
            _statusItem,
            new ToolStripSeparator(),
            _toggleItem,
            _clearItem,
            new ToolStripSeparator(),
            settingsItem,
            _autoStartItem,
            new ToolStripSeparator(),
            exitItem
        ]);

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Text = "ClipboardMerge - 普通模式",
            Visible = true,
            ContextMenuStrip = menu
        };
        _trayIcon.DoubleClick += (_, _) => OpenSettings();

        if (!_keyboardHook.Installed)
            ShowToast("快捷键监听失败", "无法启用 Win+Shift+C / Win+Shift+X。");
    }

    private void HandleClipboardChanged()
    {
        if (IsInternalClipboardUpdate())
            return;

        if (!TryGetClipboardText(out var text))
        {
            if (_collecting)
                ShowToast("累积复制", "当前仅支持合并文本内容。");
            return;
        }

        if (_collecting)
        {
            _items.Add(text);
            ResetInactivityCountdown();
            var merged = string.Join(_settings.Separator, _items);
            SetClipboardTextInternal(merged);
            UpdateTrayState();
            ShowToast($"已累积 {_items.Count} 条", MakePreview(text));
        }
        else if (_settings.ShowCopyNotification)
        {
            ShowToast("已复制", MakePreview(text));
        }
    }

    private void ToggleCollection()
    {
        if (_collecting)
            FinishCollection();
        else
            StartCollection();
    }

    private void StartCollection()
    {
        _items.Clear();
        _collecting = true;
        ResetInactivityCountdown();
        UpdateInactivityTimer();
        UpdateTrayState();
        ShowToast(
            "累积复制已开启",
            _settings.AutoFinishAfterInactivity
                ? "继续 Ctrl+C；60 秒没有新复制将自动结束，保留合并内容。"
                : "继续使用 Ctrl+C；Ctrl+V 可随时粘贴合并结果。");
    }

    private void FinishCollection(bool automatic = false)
    {
        if (!_collecting)
            return;

        _collecting = false;
        _inactivityTimer.Stop();
        UpdateTrayState();

        if (_items.Count > 0)
        {
            var merged = string.Join(_settings.Separator, _items);
            SetClipboardTextInternal(merged);
            ShowToast(
                automatic ? "60 秒未复制，已自动结束" : "累积复制已结束",
                $"共 {_items.Count} 条，合并结果已保留在剪贴板。");
        }
        else
        {
            ShowToast(
                automatic ? "60 秒未复制，已自动结束" : "累积复制已结束",
                "本次没有收集到文本，原剪贴板内容不变。");
        }
    }

    private void ClearCollection()
    {
        if (!_collecting)
        {
            ShowToast("未清空", "当前没有开启累积复制。");
            return;
        }

        _items.Clear();
        ResetInactivityCountdown();
        SetClipboardTextInternal(string.Empty);
        UpdateTrayState();
        ShowToast("已清空累计内容", "累积模式仍保持开启，可以继续 Ctrl+C。");
    }

    private void UpdateTrayState()
    {
        if (_collecting)
        {
            _statusItem.Text = $"状态：累积模式（{_items.Count} 条）";
            _toggleItem.Text = "结束累积复制    Win+Shift+C";
            _clearItem.Enabled = true;
            _trayIcon.Text = "ClipboardMerge - 累积模式";
        }
        else
        {
            _statusItem.Text = "状态：普通模式";
            _toggleItem.Text = "开始累积复制    Win+Shift+C";
            _clearItem.Enabled = false;
            _trayIcon.Text = "ClipboardMerge - 普通模式";
        }
    }

    private void OpenSettings()
    {
        var wasAutoFinishEnabled = _settings.AutoFinishAfterInactivity;
        using var form = new SettingsForm(_settings);
        if (form.ShowDialog() != DialogResult.OK)
            return;

        _settings = form.Result;
        _settings.Save();
        StartupManager.SetEnabled(_settings.AutoStart);
        _autoStartItem.Checked = _settings.AutoStart;
        if (_collecting && !wasAutoFinishEnabled && _settings.AutoFinishAfterInactivity)
            ResetInactivityCountdown();
        UpdateInactivityTimer();
        ShowToast("设置已保存", "新设置已立即生效。");
    }

    private void ResetInactivityCountdown()
    {
        _lastCollectionActivityTick = Environment.TickCount64;
    }

    private void UpdateInactivityTimer()
    {
        _inactivityTimer.Enabled = _collecting && _settings.AutoFinishAfterInactivity;
    }

    private void CheckCollectionInactivity()
    {
        if (_collecting
            && _settings.AutoFinishAfterInactivity
            && Environment.TickCount64 - _lastCollectionActivityTick >= InactivityTimeoutMilliseconds)
        {
            FinishCollection(automatic: true);
        }
    }

    private void ShowToast(string title, string text)
    {
        _toast.ShowMessage(title, text, _settings.NotificationMilliseconds, _settings.PlaySound);
    }

    private string MakePreview(string text)
    {
        var value = text.Replace("\0", string.Empty).Trim();
        if (value.Length <= _settings.PreviewLength)
            return value;
        return value[.._settings.PreviewLength] + "…";
    }

    private static bool TryGetClipboardText(out string text)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    text = Clipboard.GetText();
                    return true;
                }
                break;
            }
            catch
            {
                Thread.Sleep(20);
            }
        }

        text = string.Empty;
        return false;
    }

    private void SetClipboardTextInternal(string text)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                var data = new DataObject();
                data.SetData(DataFormats.UnicodeText, text);
                data.SetData(DataFormats.Text, text);
                data.SetData(InternalClipboardFormat, "1");
                Clipboard.SetDataObject(data, true);
                return;
            }
            catch
            {
                Thread.Sleep(20);
            }
        }
    }

    private static bool IsInternalClipboardUpdate()
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                return Clipboard.ContainsData(InternalClipboardFormat);
            }
            catch
            {
                Thread.Sleep(10);
            }
        }

        return false;
    }

    private void Exit()
    {
        _trayIcon.Visible = false;
        _inactivityTimer.Stop();
        _keyboardHook.Dispose();
        _window.Dispose();
        _toast.Close();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Dispose();
            _inactivityTimer.Dispose();
            _keyboardHook.Dispose();
            _window.Dispose();
            _toast.Dispose();
        }
        base.Dispose(disposing);
    }
}
