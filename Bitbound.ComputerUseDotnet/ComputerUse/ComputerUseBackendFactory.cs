using Bitbound.ComputerUseDotnet.ComputerUse.Linux;
using Bitbound.ComputerUseDotnet.ComputerUse.Mac;
using Bitbound.ComputerUseDotnet.ComputerUse.Portal;
using Bitbound.ComputerUseDotnet.ComputerUse.Windows;
using Bitbound.SystemAbstractions.FileSystem;
using Microsoft.Extensions.Logging;

namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>Chooses the platform-specific computer-use backend for the current session.</summary>
public static class ComputerUseBackendFactory
{
  public static IComputerUseBackend Create(IFileSystem fileSystem, ILoggerFactory loggerFactory)
  {
    ArgumentNullException.ThrowIfNull(fileSystem);
    ArgumentNullException.ThrowIfNull(loggerFactory);

    if (OperatingSystem.IsWindows())
    {
      return new WindowsBackend(loggerFactory.CreateLogger<WindowsBackend>());
    }

    if (OperatingSystem.IsMacOS())
    {
      return new MacBackend(loggerFactory.CreateLogger<MacBackend>());
    }

    var session = DesktopEnvironmentDetector.DetectLinuxSession();

    return session switch
    {
      DesktopEnvironmentType.Wayland => new WaylandBackend(
        loggerFactory.CreateLogger<WaylandBackend>(),
        fileSystem,
        new RestoreTokenStore(fileSystem, loggerFactory.CreateLogger<RestoreTokenStore>())),
      // With no session-type hints at all, prefer the portal path: it works whenever a
      // D-Bus session bus is reachable, including systemd-launched MCP clients.
      DesktopEnvironmentType.X11 => new X11Backend(loggerFactory.CreateLogger<X11Backend>()),
      _ => new WaylandBackend(
        loggerFactory.CreateLogger<WaylandBackend>(),
        fileSystem,
        new RestoreTokenStore(fileSystem, loggerFactory.CreateLogger<RestoreTokenStore>())),
    };
  }
}
