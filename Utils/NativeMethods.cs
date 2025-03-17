using System;
using System.Runtime.InteropServices;

namespace TA_WPF.Utils
{
    /// <summary>
    /// 提供对Windows API的访问
    /// </summary>
    internal static class NativeMethods
    {
        public const int GWL_STYLE = -16;
        public const int WS_MAXIMIZEBOX = 0x10000;
        public const int WM_GETMINMAXINFO = 0x0024;

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int value);
    }
} 