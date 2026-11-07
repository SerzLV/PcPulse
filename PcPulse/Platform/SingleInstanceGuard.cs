using System.Windows.Interop;

namespace PcPulse.Platform;

/// <summary>
/// Keeps PC Pulse to one copy. A second launch finds the running copy's hidden window and posts it a
/// "show yourself" message instead of starting, so the shortcut just brings PC Pulse forward (from the tray too)
/// without a UAC prompt. The running copy is elevated and the new launch is not, so the message is let through UIPI.
/// </summary>
public sealed class SingleInstanceGuard : IDisposable
{
    private const string MutexName = "PcPulse.SingleInstance";
    private const string WindowName = "PcPulse.Instance";
    private static readonly int WM_SHOW = NativeMethods.RegisterWindowMessage("PcPulse.ShowWindow");

    private readonly Mutex _mutex;
    private HwndSource? _source;
    private Action? _onShowRequested;

    private SingleInstanceGuard(Mutex mutex) => _mutex = mutex;

    /// <summary>If a copy is already running, asks it to show its window and returns true.</summary>
    public static bool NotifyRunning()
    {
        IntPtr hwnd = NativeMethods.FindWindowEx(NativeMethods.HWND_MESSAGE, IntPtr.Zero, null, WindowName);
        if (hwnd == IntPtr.Zero) return false;
        // This fresh launch holds the user's foreground rights; hand them over so the window can come to the front.
        NativeMethods.AllowSetForegroundWindow(NativeMethods.ProcessIdOf(hwnd));
        NativeMethods.PostMessage(hwnd, WM_SHOW, IntPtr.Zero, IntPtr.Zero);
        return true;
    }

    /// <summary>
    /// Claims the single-instance slot, or returns null if another copy holds it. Covers two launches at once,
    /// which <see cref="NotifyRunning"/> alone can miss.
    /// </summary>
    public static SingleInstanceGuard? TryAcquire()
    {
        try
        {
            var mutex = new Mutex(true, MutexName, out bool createdNew);
            if (createdNew) return new SingleInstanceGuard(mutex);
            mutex.Dispose();
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null; // held by an elevated copy
        }
    }

    /// <summary>Starts answering other launches' "show yourself" requests.</summary>
    public void Listen(Action onShowRequested)
    {
        _onShowRequested = onShowRequested;
        _source = new HwndSource(new HwndSourceParameters(WindowName) { ParentWindow = NativeMethods.HWND_MESSAGE, WindowStyle = 0 });
        NativeMethods.ChangeWindowMessageFilterEx(_source.Handle, WM_SHOW, NativeMethods.MSGFLT_ALLOW, IntPtr.Zero);
        _source.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_SHOW)
        {
            _onShowRequested?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        _source?.Dispose();
        _mutex.Dispose();
    }
}
