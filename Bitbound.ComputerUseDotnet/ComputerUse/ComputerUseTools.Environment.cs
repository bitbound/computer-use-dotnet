using System.Text;

namespace Bitbound.ComputerUseDotnet.ComputerUse;

public sealed partial class ComputerUseTools
{
  [McpServerTool(Name = "check_permissions")]
  [Description("Checks screen-capture and input-simulation permission state without prompting. On macOS this maps to Screen Recording / Accessibility; on Linux Wayland it reports whether a RemoteDesktop portal session is active.")]
  public async Task<string> CheckPermissions()
  {
    var status = await _backend.CheckPermissionsAsync();
    return Describe(status);
  }

  [McpServerTool(Name = "get_desktop_info")]
  [Description("Reports the desktop environment, display layout (indexes, bounds, scale), backend in use, and current permission states. Call this before taking actions to understand the coordinate space.")]
  public async Task<string> GetDesktopInfo()
  {
    var layout = await _backend.GetDisplayLayoutAsync();
    var permissions = await _backend.CheckPermissionsAsync();
    var cursor = await _backend.GetCursorPositionAsync();

    var builder = new StringBuilder();
    builder.AppendLine($"Backend: {_backend.BackendName} ({_backend.EnvironmentType})");
    builder.AppendLine($"Virtual screen: {layout.Width}x{layout.Height} (origin offset {layout.OriginX},{layout.OriginY})");
    builder.AppendLine($"Cursor: {(cursor is null ? "unsupported" : $"({cursor.Value.X}, {cursor.Value.Y})")}");
    builder.AppendLine("Displays:");

    foreach (var display in layout.Displays)
    {
      builder.AppendLine(
        $"  [{display.Index}] {display.Name} {(display.IsPrimary ? "(primary) " : string.Empty)}" +
        $"normalized=({display.X - layout.OriginX}, {display.Y - layout.OriginY}) {display.Width}x{display.Height} scale={display.Scale:0.##}");
    }

    builder.AppendLine($"Screen capture permission: {permissions.ScreenCapture}");
    builder.AppendLine($"Input simulation permission: {permissions.InputSimulation}");

    if (!string.IsNullOrWhiteSpace(permissions.Details))
    {
      builder.AppendLine($"Details: {permissions.Details}");
    }

    return builder.ToString().TrimEnd();
  }

  [McpServerTool(Name = "request_permissions")]
  [Description(
    "Triggers the platform permission prompts needed for screen capture and input simulation. " +
    "On macOS this requests Screen Recording and Accessibility (a relaunch of this server may be required after granting). " +
    "On Linux Wayland it starts an XDG Desktop Portal session, which shows a user consent dialog; the restore token is saved so later sessions do not re-prompt. " +
    "On Windows and X11 no permissions are required.")]
  public async Task<string> RequestPermissions()
  {
    var status = await _backend.RequestPermissionsAsync();
    return Describe(status);
  }

  private static string Describe(PermissionStatus status) =>
    $"Backend: {status.BackendName}\n" +
    $"Screen capture: {status.ScreenCapture}\n" +
    $"Input simulation: {status.InputSimulation}" +
    (string.IsNullOrWhiteSpace(status.Details) ? string.Empty : $"\nDetails: {status.Details}");
}
