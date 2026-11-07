using System.Windows.Interop;
using PcPulse.Core.Settings;
using PcPulse.Shell;

namespace PcPulse.Platform;

/// <summary>
/// RegisterHotKey bound to a hidden message-only window, so hotkeys work while the main window is in the tray.
/// </summary>
public sealed class GlobalHotkeyService : IHotkeyService
{
    private readonly HwndSource _source;
    private readonly Dictionary<int, Action> _actions = new();
    private int _nextId = 1;

    public GlobalHotkeyService()
    {
        _source = new HwndSource(new HwndSourceParameters("PcPulse.Hotkeys") { ParentWindow = NativeMethods.HWND_MESSAGE, WindowStyle = 0 });
        _source.AddHook(WndProc);
    }

    public bool Register(Hotkey hotkey, Action onPressed)
    {
        if (hotkey.IsEmpty) return true;
        int id = _nextId++;
        // Alt/Ctrl/Shift/Win share values with Win32's MOD_* flags.
        uint modifiers = (uint)hotkey.Modifiers | NativeMethods.MOD_NOREPEAT;
        if (!NativeMethods.RegisterHotKey(_source.Handle, id, modifiers, KeyMapping.ToVirtualKey(hotkey)))
            return false;
        _actions[id] = onPressed;
        return true;
    }

    public void UnregisterAll()
    {
        foreach (int id in _actions.Keys) NativeMethods.UnregisterHotKey(_source.Handle, id);
        _actions.Clear();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && _actions.TryGetValue(wParam.ToInt32(), out var action))
        {
            action();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        UnregisterAll();
        _source.Dispose();
    }
}
