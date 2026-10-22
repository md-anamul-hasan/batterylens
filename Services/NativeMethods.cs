using System;
using System.Runtime.InteropServices;

namespace BatteryLens.Services;

public static class NativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;          // 0: Offline, 1: Online, 255: Unknown
        public byte BatteryFlag;           // 1: High, 2: Low, 4: Critical, 8: Charging, 128: No system battery, 255: Unknown
        public byte BatteryLifePercent;    // 0-100%, 255: Unknown
        public byte SystemStatusFlag;      // 0: Saver off, 1: Saver on
        public int BatteryLifeTime;        // Seconds remaining, -1 if unknown
        public int BatteryFullLifeTime;    // Seconds for full charge, -1 if unknown
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

    // Windows 11 Desktop Window Manager (DWM) Attributes
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    public const int DWMWCP_ROUND = 2;
    public const int DWMSBT_MAINWINDOW = 2; // Mica

    [DllImport("dwmapi.dll", PreserveSig = true)]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public static void EnableWindows11DarkTheme(IntPtr hwnd)
    {
        try
        {
            int darkMode = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

            int cornerPreference = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));
        }
        catch
        {
            // Silently ignore if running on older OS versions
        }
    }
}
