namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>
/// Classifies the Linux session as Wayland or X11 from environment variables,
/// following the same precedence ControlR uses (WAYLAND_DISPLAY, then DISPLAY, then XDG_SESSION_TYPE).
/// </summary>
public static class DesktopEnvironmentDetector
{

  /// <summary>Detects the desktop environment across all operating systems.</summary>
  public static DesktopEnvironmentType DetectCurrent()
  {
    if (OperatingSystem.IsWindows())
    {
      return DesktopEnvironmentType.Windows;
    }

    if (OperatingSystem.IsMacOS())
    {
      return DesktopEnvironmentType.MacOS;
    }

    if (OperatingSystem.IsLinux())
    {
      return DetectLinuxSession();
    }

    return DesktopEnvironmentType.Unknown;
  }

  /// <summary>Detects the Linux session type using the real environment.</summary>
  public static DesktopEnvironmentType DetectLinuxSession(Func<string, string?>? getEnvironmentVariable = null) =>
    Detect(getEnvironmentVariable ?? Environment.GetEnvironmentVariable);

  private static DesktopEnvironmentType Detect(Func<string, string?> getEnvironmentVariable)
  {
    var waylandDisplay = getEnvironmentVariable("WAYLAND_DISPLAY");
    if (!string.IsNullOrWhiteSpace(waylandDisplay))
    {
      return DesktopEnvironmentType.Wayland;
    }

    var xdgSessionType = getEnvironmentVariable("XDG_SESSION_TYPE");

    var display = getEnvironmentVariable("DISPLAY");
    if (!string.IsNullOrWhiteSpace(display))
    {
      // DISPLAY alone is not decisive: XWayland clients on a Wayland session have both set.
      return string.Equals(xdgSessionType, "wayland", StringComparison.OrdinalIgnoreCase)
        ? DesktopEnvironmentType.Wayland
        : DesktopEnvironmentType.X11;
    }

    if (string.Equals(xdgSessionType, "wayland", StringComparison.OrdinalIgnoreCase))
    {
      return DesktopEnvironmentType.Wayland;
    }

    if (string.Equals(xdgSessionType, "x11", StringComparison.OrdinalIgnoreCase))
    {
      return DesktopEnvironmentType.X11;
    }

    return DesktopEnvironmentType.Unknown;
  }
}
