using System;
using System.Runtime.InteropServices;

namespace Community.PowerToys.Run.Plugin.OutlastTrials;

public class Native
{
    public static readonly IntPtr HWND_TOPMOST = new(-1);
    public static readonly uint SWP_NOSIZE = 0x0001;
    public static readonly uint SWP_NOMOVE = 0x0002;
    public static readonly uint TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags
    );
}
