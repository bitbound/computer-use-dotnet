namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>The kind of desktop session the server is running in.</summary>
public enum DesktopEnvironmentType
{
  /// <summary>Windows desktop session.</summary>
  Windows,

  /// <summary>macOS window server session.</summary>
  MacOS,

  /// <summary>Native X11 session.</summary>
  X11,

  /// <summary>Wayland session using XDG Desktop Portal for capture and input.</summary>
  Wayland,

  /// <summary>Could not be determined.</summary>
  Unknown,
}
