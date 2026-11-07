using System.Runtime.InteropServices;

namespace PcPulse.Infrastructure.Windows;

internal static class NativeMethods
{
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);
}
