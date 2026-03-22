using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Community.PowerToys.Run.Plugin.OutlastTrials;

public class Native
{
    public static readonly IntPtr HWND_TOPMOST = new(-1);
    public static readonly uint SWP_NOSIZE = 0x0001;
    public static readonly uint SWP_NOMOVE = 0x0002;
    public static readonly uint TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;

    public static readonly int SW_RESTORE = 9;

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags
    );

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public static FocusResult FocusProcess(string processName)
    {
        processName = processName.Replace(".exe", "");

        Process[] processes = Process.GetProcessesByName(processName);
        if (processes.Length == 0)
            return FocusResult.ProcessNotFound;

        IntPtr hWnd = processes[0].MainWindowHandle;
        if (hWnd == IntPtr.Zero)
            return FocusResult.NoMainWindow;

        if (!ShowWindow(hWnd, SW_RESTORE))
            return FocusResult.Failed;

        if (!SetForegroundWindow(hWnd))
            return FocusResult.Failed;

        return FocusResult.Success;
    }

    public enum FocusResult
    {
        Success,
        ProcessNotFound,
        NoMainWindow,
        Failed,
    }
}
