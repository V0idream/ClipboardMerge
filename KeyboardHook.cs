using System.Runtime.InteropServices;

namespace ClipboardMerge;

internal sealed class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private const int VK_C = 0x43;
    private const int VK_X = 0x58;
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;
    private const int VK_SHIFT = 0x10;
    private const int VK_LSHIFT = 0xA0;
    private const int VK_RSHIFT = 0xA1;

    private readonly HookProc _proc;
    private IntPtr _hook;
    private bool _winDown;
    private bool _shiftDown;
    private bool _cDown;
    private bool _xDown;

    public event EventHandler? ToggleRequested;
    public event EventHandler? ClearRequested;

    public bool Installed => _hook != IntPtr.Zero;

    public KeyboardHook()
    {
        _proc = HookCallback;
        _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
            return CallNextHookEx(_hook, nCode, wParam, lParam);

        var info = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
        var msg = wParam.ToInt32();
        var isDown = msg is WM_KEYDOWN or WM_SYSKEYDOWN;
        var isUp = msg is WM_KEYUP or WM_SYSKEYUP;
        var vk = info.vkCode;

        if (vk is VK_LWIN or VK_RWIN)
        {
            if (isDown) _winDown = true;
            if (isUp) _winDown = false;
        }
        else if (vk is VK_SHIFT or VK_LSHIFT or VK_RSHIFT)
        {
            if (isDown) _shiftDown = true;
            if (isUp) _shiftDown = false;
        }

        if (vk == VK_C && (_winDown && _shiftDown || _cDown))
        {
            if (isDown && !_cDown)
            {
                _cDown = true;
                ToggleRequested?.Invoke(this, EventArgs.Empty);
            }
            else if (isUp)
            {
                _cDown = false;
            }
            return (IntPtr)1;
        }

        if (vk == VK_X && (_winDown && _shiftDown || _xDown))
        {
            if (isDown && !_xDown)
            {
                _xDown = true;
                ClearRequested?.Invoke(this, EventArgs.Empty);
            }
            else if (isUp)
            {
                _xDown = false;
            }
            return (IntPtr)1;
        }

        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hook == IntPtr.Zero)
            return;

        UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public UIntPtr dwExtraInfo;
    }

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc callback, IntPtr module, uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? moduleName);
}
