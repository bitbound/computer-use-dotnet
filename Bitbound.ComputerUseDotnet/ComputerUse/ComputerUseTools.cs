using System.Globalization;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;

namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>
/// MCP tools that let an agent see the screen and simulate input on Windows, macOS,
/// and Linux (X11 and Wayland via the XDG Desktop Portal). Input tools take positions as
/// fractions of the desktop, where (0, 0) is the top-left of the combined displays and
/// (1, 1) is the bottom-right.
/// </summary>
[McpServerToolType]
public sealed partial class ComputerUseTools(IComputerUseBackend backend, ILogger<ComputerUseTools> logger)
{
  private readonly IComputerUseBackend _backend = backend;
  private readonly ILogger<ComputerUseTools> _logger = logger;

  /// <summary>Formats a number for tool text, which a model reads back, so it must not follow the host locale.</summary>
  private static string Format(double value, string specifier = "0.####") =>
    value.ToString(specifier, CultureInfo.InvariantCulture);

  private static MouseButton ParseMouseButton(string button) => button.Trim().ToLowerInvariant() switch
  {
    "left" => MouseButton.Left,
    "right" => MouseButton.Right,
    "middle" => MouseButton.Middle,
    "extra" or "back" => MouseButton.Extra,
    "side" or "forward" => MouseButton.Side,
    _ => throw new ArgumentException($"Unknown mouse button '{button}'. Use left, right, middle, extra, or side."),
  };

  private async Task<T> RunToolAsync<T>(string toolName, Func<Task<T>> action)
  {
    try
    {
      return await action();
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (Exception exception)
    {
      // A logger failure must not swallow the original error, or the MCP SDK falls back to its
      // generic "An error occurred invoking X" and the actionable message is lost.
      try { _logger.LogError(exception, "MCP tool {ToolName} failed.", toolName); }
      catch { /* logger failure must not prevent the McpException below */ }
      throw new McpException($"{toolName} failed: {exception.Message} (see read_logs for the full stack trace)", exception);
    }
  }
}
