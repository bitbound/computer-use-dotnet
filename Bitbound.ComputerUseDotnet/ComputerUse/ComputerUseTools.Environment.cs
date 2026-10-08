using System.Text;
using Bitbound.ComputerUseDotnet.Logging;

namespace Bitbound.ComputerUseDotnet.ComputerUse;

public sealed partial class ComputerUseTools
{
  [McpServerTool(Name = "check_permissions")]
  [Description("Checks screen-capture and input-simulation permission state without prompting. On macOS this maps to Screen Recording / Accessibility; on Linux Wayland it reports whether a RemoteDesktop portal session is active.")]
  public Task<string> CheckPermissions() => RunToolAsync("check_permissions", async () =>
  {
    var status = await _backend.CheckPermissionsAsync();
    return Describe(status);
  });

  [McpServerTool(Name = "get_desktop_info")]
  [Description("Reports the desktop environment, display layout (indexes, bounds, scale), backend in use, and current permission states. Call this before taking actions to understand the coordinate space.")]
  public Task<string> GetDesktopInfo() => RunToolAsync("get_desktop_info", async () =>
  {
    var layout = await _backend.GetDisplayLayoutAsync();
    var permissions = await _backend.CheckPermissionsAsync();
    var cursor = await _backend.GetCursorPositionAsync();

    var builder = new StringBuilder();
    builder.AppendLine($"Backend: {_backend.BackendName} ({_backend.EnvironmentType})");
    builder.AppendLine($"Desktop: {layout.Width}x{layout.Height} pixels (origin offset {layout.OriginX},{layout.OriginY})");
    builder.AppendLine("Input tools take fractions of this desktop: x 0.0 left edge to 1.0 right edge, y 0.0 top edge to 1.0 bottom edge.");
    builder.AppendLine($"Cursor: {(cursor is null ? "unsupported" : Describe(layout, cursor.Value))}");
    builder.AppendLine("Displays:");

    foreach (var display in layout.Displays)
    {
      var (min, max) = layout.FractionBoundsOf(display);
      var primary = display.IsPrimary ? "(primary) " : string.Empty;

      builder.AppendLine(
        $"  [{display.Index}] {display.Name} {primary}" +
        $"pixel=({display.X - layout.OriginX}, {display.Y - layout.OriginY}) {display.Width}x{display.Height} " +
        $"scale={Format(display.Scale, "0.##")} " +
        $"fractions x {Format(min.X)} to {Format(max.X)}, y {Format(min.Y)} to {Format(max.Y)}");
    }

    builder.AppendLine($"Screen capture permission: {permissions.ScreenCapture}");
    builder.AppendLine($"Input simulation permission: {permissions.InputSimulation}");

    if (!string.IsNullOrWhiteSpace(permissions.Details))
    {
      builder.AppendLine($"Details: {permissions.Details}");
    }

    return builder.ToString().TrimEnd();
  });

  [McpServerTool(Name = "read_logs")]
  [Description(
    "Returns recent lines from the server's rolling log file, including the full exception detail " +
    "(type, message, stack trace, inner exceptions) that the tool result truncates. The log lives at " +
    "LocalApplicationData/Bitbound/ComputerUseDotnet/Logs/computer-use-dotnet.log and rotates at 2 MB " +
    "with three backups. Call this after any tool error to see what actually failed.")]
  public Task<string> ReadLogs(
      [Description("Number of trailing lines to return. Default: 100, max: 1000.")]
        int lines = 100,
      [Description("Optional case-insensitive substring filter; only lines containing it are returned. " +
        "Use the tool name or exception type to narrow.")]
        string? contains = null) => RunToolAsync("read_logs", async () =>
  {
    return await TailLogAsync(LogPaths.CurrentFile, lines, contains);
  });

  [McpServerTool(Name = "request_permissions")]
  [Description(
    "Triggers the platform permission prompts needed for screen capture and input simulation. " +
    "On macOS this requests Screen Recording and Accessibility (a relaunch of this server may be required after granting). " +
    "On Linux Wayland it starts an XDG Desktop Portal session, which shows a user consent dialog; the restore token is saved so later sessions do not re-prompt. " +
    "On Windows and X11 no permissions are required.")]
  public Task<string> RequestPermissions() => RunToolAsync("request_permissions", async () =>
  {
    var status = await _backend.RequestPermissionsAsync();
    return Describe(status);
  });

  internal static async Task<string> TailLogAsync(string path, int lines, string? contains)
  {
    if (!File.Exists(path))
    {
      return $"No log file at {path} yet. Tool errors will be appended there as they happen.";
    }

    var all = await File.ReadAllLinesAsync(path);
    var take = Math.Clamp(lines, 1, 1000);
    var filtered = string.IsNullOrEmpty(contains)
      ? all
      : all.Where(l => l.Contains(contains, StringComparison.OrdinalIgnoreCase)).ToArray();

    if (filtered.Length == 0)
    {
      return $"No log lines matched (file {path}, {all.Length} total lines).";
    }

    var tail = filtered.Skip(Math.Max(0, filtered.Length - take)).ToArray();
    return $"Log file: {path} ({all.Length} total lines, showing last {tail.Length}).\n" + string.Join("\n", tail);
  }

  private static string Describe(PermissionStatus status) =>
    $"Backend: {status.BackendName}\n" +
    $"Screen capture: {status.ScreenCapture}\n" +
    $"Input simulation: {status.InputSimulation}" +
    (string.IsNullOrWhiteSpace(status.Details) ? string.Empty : $"\nDetails: {status.Details}");
}
