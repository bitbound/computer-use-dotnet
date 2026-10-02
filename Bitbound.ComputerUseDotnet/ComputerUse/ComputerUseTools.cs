using Microsoft.Extensions.Logging;

namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>
/// MCP tools that let an agent see the screen and simulate input on Windows, macOS,
/// and Linux (X11 and Wayland via the XDG Desktop Portal).
/// Coordinates are logical pixels in virtual-screen space: the same pixel grid as a
/// full <c>take_screenshot</c> image, with (0,0) at the top-left of the combined displays.
/// </summary>
[McpServerToolType]
public sealed partial class ComputerUseTools
{
  private readonly IComputerUseBackend _backend;
  private readonly ILogger<ComputerUseTools> _logger;

  public ComputerUseTools(IComputerUseBackend backend, ILogger<ComputerUseTools> logger)
  {
    _backend = backend;
    _logger = logger;
  }

  private static ScreenPoint ToPoint(int x, int y) => new(x, y);

  private static MouseButton ParseMouseButton(string button) => button.Trim().ToLowerInvariant() switch
  {
    "left" => MouseButton.Left,
    "right" => MouseButton.Right,
    "middle" => MouseButton.Middle,
    "extra" or "back" => MouseButton.Extra,
    "side" or "forward" => MouseButton.Side,
    _ => throw new ArgumentException($"Unknown mouse button '{button}'. Use left, right, middle, extra, or side."),
  };
}
