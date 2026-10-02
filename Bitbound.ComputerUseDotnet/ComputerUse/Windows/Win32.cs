using System.Runtime.InteropServices;
using System.Text;

namespace Bitbound.ComputerUseDotnet.ComputerUse.Windows;

/// <summary>Hand-written Win32 bindings for GDI screen capture and SendInput simulation.</summary>
internal static unsafe class Win32
{
  public const int SM_XVIRTUALSCREEN = 76;
  public const int SM_YVIRTUALSCREEN = 77;
  public const int SM_CXVIRTUALSCREEN = 78;
  public const int SM_CYVIRTUALSCREEN = 79;

  public const uint SRCCOPY = 0x00CC0020;
  public const uint CAPTUREBLT = 0x40000000;
  public const uint DIB_RGB_COLORS = 0;

  public const uint INPUT_MOUSE = 0;
  public const uint INPUT_KEYBOARD = 1;

  public const uint MOUSEEVENTF_MOVE = 0x0001;
  public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
  public const uint MOUSEEVENTF_LEFTUP = 0x0004;
  public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
  public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
  public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
  public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
  public const uint MOUSEEVENTF_XDOWN = 0x0080;
  public const uint MOUSEEVENTF_XUP = 0x0100;
  public const uint MOUSEEVENTF_WHEEL = 0x0800;
  public const uint MOUSEEVENTF_HWHEEL = 0x1000;

  public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
  public const uint KEYEVENTF_KEYUP = 0x0002;
  public const uint KEYEVENTF_UNICODE = 0x0004;

  public const int WHEEL_DELTA = 120;
  public const int MONITORINFOF_PRIMARY = 1;

  public const nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;

  public delegate bool MonitorEnumProc(nint hMonitor, nint hdcMonitor, RECT* lprcMonitor, nint dwData);

  private const string User32 = "user32.dll";
  private const string Gdi32 = "gdi32.dll";
  private const string Shcore = "shcore.dll";

  [DllImport(User32)]
  public static extern bool SetProcessDpiAwarenessContext(nint value);

  [DllImport(User32)]
  public static extern bool SetProcessDPIAware();

  [DllImport(User32)]
  public static extern int GetSystemMetrics(int nIndex);

  [DllImport(User32)]
  public static extern nint GetDC(nint hWnd);

  [DllImport(User32)]
  public static extern int ReleaseDC(nint hWnd, nint hDC);

  [DllImport(User32)]
  public static extern bool GetCursorPos(out POINT point);

  [DllImport(User32, SetLastError = true)]
  public static extern bool SetCursorPos(int x, int y);

  [DllImport(User32, SetLastError = true)]
  public static extern uint SendInput(uint cInputs, INPUT* pInputs, int cbSize);

  [DllImport(User32, CharSet = CharSet.Unicode)]
  public static extern bool EnumDisplayMonitors(nint hdc, RECT* lprcClip, MonitorEnumProc lpfnEnum, nint dwData);

  [DllImport(User32, CharSet = CharSet.Unicode)]
  public static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFOEX lpmi);

  [DllImport(Gdi32)]
  public static extern nint CreateCompatibleDC(nint hdc);

  [DllImport(Gdi32)]
  public static extern nint CreateCompatibleBitmap(nint hdc, int width, int height);

  [DllImport(Gdi32)]
  public static extern nint SelectObject(nint hdc, nint obj);

  [DllImport(Gdi32, SetLastError = true)]
  public static extern bool BitBlt(nint hdc, int x, int y, int cx, int cy, nint hdcSrc, int x1, int y1, uint rop);

  [DllImport(Gdi32)]
  public static extern bool DeleteDC(nint hdc);

  [DllImport(Gdi32)]
  public static extern bool DeleteObject(nint obj);

  [DllImport(Gdi32)]
  public static extern int GetDIBits(nint hdc, nint hbmp, uint uStartScan, uint cScanLines, byte* lpvBits, ref BITMAPINFO lpbi, uint uUsage);

  [DllImport(Shcore)]
  public static extern int GetDpiForMonitor(nint hmonitor, int dpiType, out uint dpiX, out uint dpiY);

  /// <summary>Best-effort per-monitor v2 DPI awareness so metrics and BitBlt use physical pixels.</summary>
  public static void TryEnablePerMonitorDpiAwareness()
  {
    try
    {
      if (!SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
      {
        SetProcessDPIAware();
      }
    }
    catch (EntryPointNotFoundException)
    {
      SetProcessDPIAware();
    }
    catch (DllNotFoundException)
    {
      SetProcessDPIAware();
    }
  }

  public static uint SendInputs(params INPUT[] inputs)
  {
    fixed (INPUT* pointer = inputs)
    {
      return SendInput((uint)inputs.Length, pointer, sizeof(INPUT));
    }
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct POINT
  {
    public int X;
    public int Y;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct RECT
  {
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct INPUT
  {
    public uint Type;
    public InputUnion Union;
  }

  [StructLayout(LayoutKind.Explicit)]
  public struct InputUnion
  {
    [FieldOffset(0)] public MOUSEINPUT Mouse;
    [FieldOffset(0)] public KEYBDINPUT Keyboard;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct MOUSEINPUT
  {
    public int Dx;
    public int Dy;
    public uint MouseData;
    public uint Flags;
    public uint Time;
    public nint ExtraInfo;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct KEYBDINPUT
  {
    public ushort VirtualKey;
    public ushort Scan;
    public uint Flags;
    public uint Time;
    public nint ExtraInfo;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct BITMAPINFOHEADER
  {
    public uint Size;
    public int Width;
    public int Height;
    public ushort Planes;
    public ushort BitCount;
    public uint Compression;
    public uint SizeImage;
    public int XPelsPerMeter;
    public int YPelsPerMeter;
    public uint ClrUsed;
    public uint ClrImportant;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct BITMAPINFO
  {
    public BITMAPINFOHEADER Header;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  public struct MONITORINFOEX
  {
    public uint Size;
    public RECT Monitor;
    public RECT Work;
    public uint Flags;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string DeviceName;
  }
}
