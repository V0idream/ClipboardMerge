namespace ClipboardMerge;

internal sealed class MessageWindow : Form
{
    private bool _monitoring = true;

    public event EventHandler? ClipboardChanged;

    public MessageWindow()
    {
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(-32000, -32000);
        Size = new Size(1, 1);
        Opacity = 0;

        _ = Handle;
        NativeMethods.AddClipboardFormatListener(Handle);
    }

    protected override void SetVisibleCore(bool value) => base.SetVisibleCore(false);

    public void SetClipboardMonitoring(bool enabled)
    {
        if (_monitoring == enabled || !IsHandleCreated)
            return;

        if (enabled)
            NativeMethods.AddClipboardFormatListener(Handle);
        else
            NativeMethods.RemoveClipboardFormatListener(Handle);

        _monitoring = enabled;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_CLIPBOARDUPDATE)
            ClipboardChanged?.Invoke(this, EventArgs.Empty);

        base.WndProc(ref m);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && IsHandleCreated)
        {
            if (_monitoring)
                NativeMethods.RemoveClipboardFormatListener(Handle);
        }

        base.Dispose(disposing);
    }
}
