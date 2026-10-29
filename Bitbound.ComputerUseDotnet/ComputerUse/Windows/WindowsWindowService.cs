using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Bitbound.ComputerUseDotnet.ComputerUse.Windows;

/// <summary>
/// Enumerates top-level windows and captures a single window with PrintWindow.
/// PrintWindow renders from the window's own device context, so it works even when
/// the window is covered by others. Hardware-accelerated content (GPU-composited
/// surfaces) may still come back blank.
/// </summary>
public sealed class WindowsWindowService
{
  private const int ClassNameCapacity = 256;

  public WindowsWindowService()
  {
    // Unvirtualized coordinates and physical-pixel captures require DPI awareness.
    Win32.TryEnablePerMonitorDpiAwareness();
  }

  public static string FormatHandle(nint handle) => $"0x{(long)handle:X}";

  /// <summary>Captures one window with PrintWindow and returns its pixels as a BGRA bitmap.</summary>
  public unsafe SKBitmap CaptureWindow(nint handle)
  {
    if (!Win32.IsWindow(handle))
    {
      throw new InvalidOperationException($"No live window with handle {FormatHandle(handle)}.");
    }

    if (!TryGetBounds(handle, out var bounds))
    {
      throw new InvalidOperationException($"GetWindowRect failed for {FormatHandle(handle)} with Win32 error {Marshal.GetLastWin32Error()}.");
    }

    var width = bounds.Right - bounds.Left;
    var height = bounds.Bottom - bounds.Top;

    if (width <= 0 || height <= 0)
    {
      throw new InvalidOperationException($"Window {FormatHandle(handle)} has no area. It is probably minimized; restore it first.");
    }

    var windowDc = Win32.GetWindowDC(handle);

    if (windowDc == nint.Zero)
    {
      throw new InvalidOperationException($"GetWindowDC failed for {FormatHandle(handle)} with Win32 error {Marshal.GetLastWin32Error()}.");
    }

    var memoryDc = Win32.CreateCompatibleDC(windowDc);
    var bitmapHandle = Win32.CreateCompatibleBitmap(windowDc, width, height);
    var originalObject = nint.Zero;

    try
    {
      if (memoryDc == nint.Zero || bitmapHandle == nint.Zero)
      {
        throw new InvalidOperationException("Failed to create the capture memory DC/bitmap.");
      }

      originalObject = Win32.SelectObject(memoryDc, bitmapHandle);

      // PW_RENDERFULLCONTENT renders DirectComposition content on Windows 8.1+;
      // plain PrintWindow is the fallback for windows that reject the flag.
      if (!Win32.PrintWindow(handle, memoryDc, Win32.PW_RENDERFULLCONTENT) &&
          !Win32.PrintWindow(handle, memoryDc, 0))
      {
        throw new InvalidOperationException($"PrintWindow failed for {FormatHandle(handle)} with Win32 error {Marshal.GetLastWin32Error()}.");
      }

      Win32.SelectObject(memoryDc, originalObject);
      originalObject = nint.Zero;

      var info = default(Win32.BITMAPINFO);
      info.Header.Size = (uint)Marshal.SizeOf<Win32.BITMAPINFOHEADER>();
      info.Header.Width = width;
      info.Header.Height = -height; // top-down rows
      info.Header.Planes = 1;
      info.Header.BitCount = 32;
      info.Header.Compression = 0; // BI_RGB

      var buffer = new byte[width * height * 4];
      int lines;

      fixed (byte* bufferPointer = buffer)
      {
        lines = Win32.GetDIBits(windowDc, bitmapHandle, 0, (uint)height, bufferPointer, ref info, Win32.DIB_RGB_COLORS);
      }

      if (lines != height)
      {
        throw new InvalidOperationException($"GetDIBits returned {lines} of {height} scan lines.");
      }

      var result = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);

      fixed (byte* bufferPointer = buffer)
      {
        var pixels = result.GetPixels();

        if (pixels == nint.Zero)
        {
          result.Dispose();
          throw new InvalidOperationException("Failed to allocate bitmap pixels.");
        }

        Buffer.MemoryCopy(bufferPointer, pixels.ToPointer(), result.ByteCount, buffer.Length);
      }

      return result;
    }
    finally
    {
      if (originalObject != nint.Zero)
      {
        Win32.SelectObject(memoryDc, originalObject);
      }

      if (bitmapHandle != nint.Zero)
      {
        Win32.DeleteObject(bitmapHandle);
      }

      if (memoryDc != nint.Zero)
      {
        Win32.DeleteDC(memoryDc);
      }

      Win32.ReleaseDC(handle, windowDc);
    }
  }

  /// <summary>Describes one window, or null when the handle is no longer a live window.</summary>
  public WindowInfo? DescribeWindow(nint handle)
  {
    if (!Win32.IsWindow(handle))
    {
      return null;
    }

    return TryDescribe(handle, Win32.GetForegroundWindow(), []);
  }

  /// <summary>
  /// Enumerates top-level windows. By default only alt-tab-eligible windows are returned:
  /// visible, uncloaked, titled, not tool windows, and not owned by another window.
  /// </summary>
  public IReadOnlyList<WindowInfo> EnumerateWindows(bool includeInvisible = false, bool includeToolWindows = false)
  {
    var foreground = Win32.GetForegroundWindow();
    var windows = new List<WindowInfo>();
    var processNames = new Dictionary<int, string?>();

    Win32.EnumWindowsProc callback = (handle, _) =>
    {
      var info = TryDescribe(handle, foreground, processNames);

      if (info is null || info.Title.Length == 0)
      {
        return true;
      }

      if (!includeInvisible && (!info.IsVisible || info.IsCloaked || info.Width <= 0 || info.Height <= 0))
      {
        return true;
      }

      if (!includeToolWindows && (info.IsToolWindow || info.HasOwner))
      {
        return true;
      }

      windows.Add(info);
      return true;
    };

    if (!Win32.EnumWindows(callback, nint.Zero))
    {
      throw new InvalidOperationException($"EnumWindows failed with Win32 error {Marshal.GetLastWin32Error()}.");
    }

    return windows;
  }

  /// <summary>All windows (any state) whose title contains the given substring, case-insensitive.</summary>
  public IReadOnlyList<WindowInfo> FindByTitle(string titleSubstring)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(titleSubstring);

    return EnumerateWindows(includeInvisible: true, includeToolWindows: true)
      .Where(w => w.Title.Contains(titleSubstring, StringComparison.OrdinalIgnoreCase))
      .ToArray();
  }

  /// <summary>
  /// Turns a tool argument into one window. A decimal or 0x-prefixed value names a handle;
  /// anything else is a title substring matched against all windows, preferring exact title,
  /// then the foreground window, then visibility, then area.
  /// </summary>
  public WindowInfo Resolve(string window)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(window);

    if (TryParseHandle(window, out var handle))
    {
      return DescribeWindow(handle)
        ?? throw new InvalidOperationException($"No live window with handle {FormatHandle(handle)}. Call list_windows for current handles.");
    }

    var candidates = FindByTitle(window);

    if (candidates.Count == 0)
    {
      throw new InvalidOperationException($"No window title contains '{window}'. Call list_windows to see the current windows.");
    }

    return SelectBestMatch(candidates, Win32.GetForegroundWindow(), window);
  }

  internal static WindowInfo SelectBestMatch(IReadOnlyList<WindowInfo> candidates, nint foregroundHandle, string query) =>
    candidates.OrderByDescending(c => string.Equals(c.Title, query, StringComparison.OrdinalIgnoreCase))
      .ThenByDescending(c => c.Handle == foregroundHandle)
      .ThenByDescending(c => c.IsVisible && !c.IsCloaked)
      .ThenByDescending(c => c.Area)
      .First();

  internal static bool TryParseHandle(string text, out nint handle)
  {
    handle = default;
    var trimmed = text.Trim();

    if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
    {
      if (!long.TryParse(trimmed.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
      {
        return false;
      }

      handle = (nint)hex;
      return true;
    }

    if (!long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
    {
      return false;
    }

    handle = (nint)value;
    return true;
  }

  private static string? GetProcessName(int processId, Dictionary<int, string?> cache)
  {
    if (cache.TryGetValue(processId, out var cached))
    {
      return cached;
    }

    string? name = null;

    try
    {
      name = Process.GetProcessById(processId).ProcessName;
    }
    catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NotSupportedException)
    {
      // Process exited or name is not readable; report it as unknown.
    }

    cache[processId] = name;
    return name;
  }

  private static WindowInfo? TryDescribe(nint handle, nint foreground, Dictionary<int, string?> processNames)
  {
    if (!TryGetBounds(handle, out var bounds))
    {
      return null;
    }

    var titleLength = Win32.GetWindowTextLengthW(handle);
    var titleBuilder = new StringBuilder(titleLength + 1);
    Win32.GetWindowTextW(handle, titleBuilder, titleBuilder.Capacity);

    var classBuilder = new StringBuilder(ClassNameCapacity);
    Win32.GetClassNameW(handle, classBuilder, classBuilder.Capacity);

    Win32.GetWindowThreadProcessId(handle, out var processId);
    var styles = Win32.GetWindowLongPtrW(handle, Win32.GWLP_EXSTYLE);

    var cloaked = 0;
    Win32.DwmGetWindowAttribute(handle, Win32.DWMWA_CLOAKED, out cloaked, sizeof(int));

    return new WindowInfo
    {
      Handle = handle,
      Title = titleBuilder.ToString(),
      ClassName = classBuilder.ToString(),
      ProcessId = (int)processId,
      ProcessName = GetProcessName((int)processId, processNames),
      X = bounds.Left,
      Y = bounds.Top,
      Width = bounds.Right - bounds.Left,
      Height = bounds.Bottom - bounds.Top,
      IsVisible = Win32.IsWindowVisible(handle),
      IsMinimized = Win32.IsIconic(handle),
      IsForeground = handle == foreground,
      IsCloaked = cloaked != 0,
      IsToolWindow = (styles.ToInt64() & Win32.WS_EX_TOOLWINDOW) != 0,
      HasOwner = Win32.GetWindowLongPtrW(handle, Win32.GWLP_HWNDPARENT) != nint.Zero,
    };
  }

  private static bool TryGetBounds(nint handle, out Win32.RECT bounds)
  {
    // The DWM frame bounds exclude the invisible resize border, so they match what the user sees.
    if (Win32.DwmGetWindowAttribute(handle, Win32.DWMWA_EXTENDED_FRAME_BOUNDS, out Win32.RECT frame, Marshal.SizeOf<Win32.RECT>()) == 0)
    {
      bounds = frame;
      return true;
    }

    return Win32.GetWindowRect(handle, out bounds);
  }
}
