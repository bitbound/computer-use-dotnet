namespace Bitbound.ComputerUseDotnet.ComputerUse.Windows;

/// <summary>
/// A top-level desktop window described in physical screen pixels (the process is
/// per-monitor DPI aware, so window coordinates are not virtualized).
/// </summary>
public sealed record WindowInfo
{
  /// <summary>Win32 window handle (HWND).</summary>
  public required nint Handle { get; init; }

  /// <summary>Window title, possibly empty for windows shown with <c>include_invisible</c>.</summary>
  public required string Title { get; init; }

  /// <summary>Window class name registered by the creating code.</summary>
  public required string ClassName { get; init; }

  /// <summary>Process that owns the window.</summary>
  public required int ProcessId { get; init; }

  /// <summary>Process name, or null when it could not be read.</summary>
  public string? ProcessName { get; init; }

  /// <summary>X of the window bounds in physical screen pixels.</summary>
  public required int X { get; init; }

  /// <summary>Y of the window bounds in physical screen pixels.</summary>
  public required int Y { get; init; }

  /// <summary>Window width in physical pixels.</summary>
  public required int Width { get; init; }

  /// <summary>Window height in physical pixels.</summary>
  public required int Height { get; init; }

  public bool IsVisible { get; init; }

  public bool IsMinimized { get; init; }

  public bool IsForeground { get; init; }

  /// <summary>Hidden by the desktop window manager (for example a suspended UWP window).</summary>
  public bool IsCloaked { get; init; }

  /// <summary>Has WS_EX_TOOLWINDOW, so it stays out of the alt-tab list.</summary>
  public bool IsToolWindow { get; init; }

  /// <summary>Owned by another window, so it stays out of the alt-tab list.</summary>
  public bool HasOwner { get; init; }

  public int Right => X + Width;

  public int Bottom => Y + Height;

  public long Area => (long)Width * Height;
}
