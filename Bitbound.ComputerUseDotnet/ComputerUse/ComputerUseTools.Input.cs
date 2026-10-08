using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Bitbound.ComputerUseDotnet.ComputerUse;

public sealed partial class ComputerUseTools
{
  [McpServerTool(Name = "click")]
  [Description(
    "Moves the pointer to a position on the desktop and clicks a mouse button. Positions are fractions of " +
    "the whole desktop: x runs 0.0 at the left edge to 1.0 at the right edge, y runs 0.0 at the top to 1.0 " +
    "at the bottom. A fraction names the same place whatever size the screenshot you looked at was, so these " +
    "coordinates are unaffected by downscaling. Values from 1 to 1000 are read as thousandths.")]
  public Task<string> Click(
      [Description("X position as a fraction of desktop width: 0.0 left edge, 0.5 center, 1.0 right edge.")]
        double x,
      [Description("Y position as a fraction of desktop height: 0.0 top edge, 0.5 center, 1.0 bottom edge.")]
        double y,
      [Description("Mouse button: left, right, middle, extra, or side. Default: left.")]
        string button = "left",
      [Description("Number of clicks (2 for a double-click). Default: 1.")]
        int click_count = 1) => RunToolAsync("click", async () =>
  {
    var layout = await _backend.GetDisplayLayoutAsync();

    if (!TryResolvePoint(x, y, layout, out var point, out var rejection))
    {
      return rejection + " No click was sent.";
    }

    var parsed = ParseMouseButton(button);
    await _backend.ClickAsync(point, parsed, Math.Clamp(click_count, 1, 5));

    return $"Clicked {button} {click_count} time(s) {Describe(layout, point)}.";
  });

  [McpServerTool(Name = "drag")]
  [Description(
    "Presses a mouse button at one desktop position, moves to another in steps, then releases. Useful for " +
    "text selection, sliders, and window moves. Positions are fractions of the whole desktop, x 0.0 left to " +
    "1.0 right and y 0.0 top to 1.0 bottom. Values from 1 to 1000 are read as thousandths.")]
  public Task<string> Drag(
      [Description("Start X as a fraction of desktop width: 0.0 left edge, 1.0 right edge.")]
        double start_x,
      [Description("Start Y as a fraction of desktop height: 0.0 top edge, 1.0 bottom edge.")]
        double start_y,
      [Description("End X as a fraction of desktop width: 0.0 left edge, 1.0 right edge.")]
        double end_x,
      [Description("End Y as a fraction of desktop height: 0.0 top edge, 1.0 bottom edge.")]
        double end_y,
      [Description("Mouse button held during the drag: left, right, middle, extra, or side. Default: left.")]
        string button = "left",
      [Description("Number of intermediate move steps. Default: 10.")]
        int steps = 10) => RunToolAsync("drag", async () =>
  {
    var layout = await _backend.GetDisplayLayoutAsync();

    if (!TryResolvePoint(start_x, start_y, layout, out var from, out var rejection) ||
        !TryResolvePoint(end_x, end_y, layout, out var to, out rejection))
    {
      return rejection + " No drag was sent.";
    }

    var parsed = ParseMouseButton(button);
    await _backend.DragAsync(from, to, parsed, Math.Clamp(steps, 1, 100));

    return $"Dragged {button} {Describe(layout, from)} to {Describe(layout, to)}.";
  });

  [McpServerTool(Name = "get_cursor_position")]
  [Description("Returns the current pointer position as a desktop fraction and in pixels, if the platform supports querying it. Compare it with the position you asked for to check your own coordinate mapping.")]
  public Task<string> GetCursorPosition() => RunToolAsync("get_cursor_position", async () =>
  {
    var layout = await _backend.GetDisplayLayoutAsync();
    var position = await _backend.GetCursorPositionAsync();

    return position is null
      ? "Cursor position query is not supported on this platform."
      : $"Cursor is {Describe(layout, position.Value)}.";
  });

  [McpServerTool(Name = "move_mouse")]
  [Description(
    "Moves the mouse pointer to a desktop position without clicking. Positions are fractions of the whole " +
    "desktop, x 0.0 left to 1.0 right and y 0.0 top to 1.0 bottom. Values from 1 to 1000 are read as thousandths.")]
  public Task<string> MoveMouse(
      [Description("X position as a fraction of desktop width: 0.0 left edge, 0.5 center, 1.0 right edge.")]
        double x,
      [Description("Y position as a fraction of desktop height: 0.0 top edge, 0.5 center, 1.0 bottom edge.")]
        double y) => RunToolAsync("move_mouse", async () =>
  {
    var layout = await _backend.GetDisplayLayoutAsync();

    if (!TryResolvePoint(x, y, layout, out var point, out var rejection))
    {
      return rejection + " No pointer move was sent.";
    }

    await _backend.MovePointerAsync(point);

    return $"Pointer moved {Describe(layout, point)}.";
  });

  [McpServerTool(Name = "press_key")]
  [Description(
    "Presses a key or key chord. Tokens are joined with '+': modifiers (ctrl, alt, shift, meta/cmd/win/super) " +
    "plus one target key name or single character. Examples: enter, tab, escape, f5, up, " +
    "ctrl+alt+delete, cmd+space, ctrl+shift+t. Name 'space' types a space bar press.")]
  public Task<string> PressKey(
      [Description("The key chord to press, e.g. 'ctrl+c' or 'enter'.")]
        string keys) => RunToolAsync("press_key", async () =>
  {
    var chord = KeyChordParser.Parse(keys);
    await _backend.PressChordAsync(chord);

    return $"Pressed {chord}.";
  });

  [McpServerTool(Name = "scroll")]
  [Description(
    "Moves the pointer to a desktop position and scrolls the mouse wheel. Positive scroll_y scrolls up, " +
    "positive scroll_x scrolls right. Positions are fractions of the whole desktop, x 0.0 left to 1.0 right " +
    "and y 0.0 top to 1.0 bottom. Values from 1 to 1000 are read as thousandths.")]
  public Task<string> Scroll(
      [Description("X position as a fraction of desktop width: 0.0 left edge, 0.5 center, 1.0 right edge.")]
        double x,
      [Description("Y position as a fraction of desktop height: 0.0 top edge, 0.5 center, 1.0 bottom edge.")]
        double y,
      [Description("Vertical wheel clicks; positive scrolls up, negative scrolls down. Default: 0.")]
        int scroll_y = 0,
      [Description("Horizontal wheel clicks; positive scrolls right, negative scrolls left. Default: 0.")]
        int scroll_x = 0) => RunToolAsync("scroll", async () =>
  {
    var layout = await _backend.GetDisplayLayoutAsync();

    if (!TryResolvePoint(x, y, layout, out var point, out var rejection))
    {
      return rejection + " No scroll was sent.";
    }

    await _backend.ScrollAsync(point, scroll_y, scroll_x);

    return $"Scrolled ({scroll_y} vertical, {scroll_x} horizontal) {Describe(layout, point)}.";
  });

  [McpServerTool(Name = "type_text")]
  [Description("Types literal text at the current keyboard focus. Prefer this for content; use press_key for shortcuts and special keys.")]
  public Task<string> TypeText(
      [Description("The text to type. Newlines produce Enter presses.")]
        string text) => RunToolAsync("type_text", async () =>
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(text);
    await _backend.TypeTextAsync(text);
    _logger.LogInformation("Typed {Length} characters.", text.Length);

    return $"Typed {text.Length} character(s).";
  });

  /// <summary>
  /// Turns caller coordinates into a virtual-screen point. Accepts fractions of 0 to 1, plus values from 1 to
  /// 1000 as thousandths because models trained on a 0-1000 grid cannot be naming pixels; no desktop is that
  /// small. Anything else comes back as text the caller can act on, since exception messages never reach
  /// an MCP caller.
  /// </summary>
  internal static bool TryResolvePoint(
      double x,
      double y,
      DisplayLayout layout,
      out ScreenPoint point,
      [NotNullWhen(false)] out string? rejection)
  {
    rejection = null;
    point = new ScreenPoint(0, 0);
    string? rejectionY = null;

    if (!TryToFraction(x, "x", layout, out double fractionX, out string? rejectionX) ||
        !TryToFraction(y, "y", layout, out double fractionY, out rejectionY))
    {
      rejection = rejectionX ?? rejectionY ?? $"{layout.Width}x{layout.Height} desktop position is out of range.";
      return false;
    }

    point = layout.FromNormalized(new NormalizedPoint(fractionX, fractionY));
    return true;
  }

  /// <summary>Describes a resolved point as both a desktop fraction and a pixel, for caller self-correction.</summary>
  private static string Describe(DisplayLayout layout, ScreenPoint point)
  {
    var fraction = layout.ToNormalized(point);
    var pixel = $"pixel {point.X},{point.Y} of {layout.Width}x{layout.Height}";
    return $"at x={Format(fraction.X)} y={Format(fraction.Y)} ({pixel})";
  }

  private static bool TryToFraction(
      double value,
      string axis,
      DisplayLayout layout,
      out double fraction,
      [NotNullWhen(false)] out string? rejection)
  {
    fraction = 0;
    rejection = null;

    if (double.IsFinite(value) && value is >= 0 and <= 1)
    {
      fraction = value;
      return true;
    }

    if (double.IsFinite(value) && value is > 1 and <= 1000)
    {
      fraction = value / 1000;
      return true;
    }

    var (lowEdge, highEdge, span) = axis switch
    {
      "x" => ("left", "right", Math.Max(1, layout.Width - 1)),
      _ => ("top", "bottom", Math.Max(1, layout.Height - 1)),
    };

    var desktop = $"{layout.Width}x{layout.Height} desktop";
    rejection =
      $"{axis}={Format(value)} is out of range. " +
      $"Send {axis} from 0.0 ({lowEdge} edge) to 1.0 ({highEdge} edge) of the {desktop}.";

    if (double.IsFinite(value) && value > 1)
    {
      var asFraction = value / span;
      rejection += asFraction <= 1
        ? $" Pixel {Format(value, "0")} is {axis}={Format(asFraction, "0.###")}."
        : $" Pixels on this axis run 0 to {span}.";
    }

    return false;
  }
}
