using Microsoft.Extensions.Logging;

namespace Bitbound.ComputerUseDotnet.ComputerUse;

public sealed partial class ComputerUseTools
{
  [McpServerTool(Name = "click")]
  [Description("Moves the pointer to a virtual-screen coordinate and clicks a mouse button.")]
  public async Task<string> Click(
      [Description("X coordinate in virtual-screen pixels.")]
        int x,
      [Description("Y coordinate in virtual-screen pixels.")]
        int y,
      [Description("Mouse button: left, right, middle, extra, or side. Default: left.")]
        string button = "left",
      [Description("Number of clicks (2 for a double-click). Default: 1.")]
        int click_count = 1)
  {
    var layout = await _backend.GetDisplayLayoutAsync();
    var point = layout.Clamp(ToPoint(x, y));
    var parsed = ParseMouseButton(button);
    await _backend.ClickAsync(point, parsed, Math.Clamp(click_count, 1, 5));
    return $"Clicked {button} {click_count} time(s) at ({point.X}, {point.Y}).";
  }

  [McpServerTool(Name = "drag")]
  [Description("Presses a mouse button at one virtual-screen coordinate, moves to another in steps, then releases. Useful for text selection, sliders, and window moves.")]
  public async Task<string> Drag(
      [Description("Start X coordinate in virtual-screen pixels.")]
        int start_x,
      [Description("Start Y coordinate in virtual-screen pixels.")]
        int start_y,
      [Description("End X coordinate in virtual-screen pixels.")]
        int end_x,
      [Description("End Y coordinate in virtual-screen pixels.")]
        int end_y,
      [Description("Mouse button held during the drag: left, right, middle, extra, or side. Default: left.")]
        string button = "left",
      [Description("Number of intermediate move steps. Default: 10.")]
        int steps = 10)
  {
    var layout = await _backend.GetDisplayLayoutAsync();
    var from = layout.Clamp(ToPoint(start_x, start_y));
    var to = layout.Clamp(ToPoint(end_x, end_y));
    var parsed = ParseMouseButton(button);
    await _backend.DragAsync(from, to, parsed, Math.Clamp(steps, 1, 100));
    return $"Dragged {button} from ({from.X}, {from.Y}) to ({to.X}, {to.Y}).";
  }

  [McpServerTool(Name = "get_cursor_position")]
  [Description("Returns the current pointer position in virtual-screen pixel coordinates, if the platform supports querying it.")]
  public async Task<string> GetCursorPosition()
  {
    var position = await _backend.GetCursorPositionAsync();

    return position is null
      ? "Cursor position query is not supported on this platform."
      : $"Cursor is at ({position.Value.X}, {position.Value.Y}).";
  }

  [McpServerTool(Name = "move_mouse")]
  [Description("Moves the mouse pointer to an absolute virtual-screen coordinate (pixel space of a full take_screenshot image).")]
  public async Task<string> MoveMouse(
      [Description("X coordinate in virtual-screen pixels.")]
        int x,
      [Description("Y coordinate in virtual-screen pixels.")]
        int y)
  {
    var layout = await _backend.GetDisplayLayoutAsync();
    var point = layout.Clamp(ToPoint(x, y));
    await _backend.MovePointerAsync(point);
    return $"Pointer moved to ({point.X}, {point.Y}).";
  }

  [McpServerTool(Name = "press_key")]
  [Description(
    "Presses a key or key chord. Tokens are joined with '+': modifiers (ctrl, alt, shift, meta/cmd/win/super) " +
    "plus one target key name or single character. Examples: enter, tab, escape, f5, up, " +
    "ctrl+alt+delete, cmd+space, ctrl+shift+t. Name 'space' types a space bar press.")]
  public async Task<string> PressKey(
      [Description("The key chord to press, e.g. 'ctrl+c' or 'enter'.")]
        string keys)
  {
    var chord = KeyChordParser.Parse(keys);
    await _backend.PressChordAsync(chord);
    return $"Pressed {chord}.";
  }

  [McpServerTool(Name = "scroll")]
  [Description("Moves the pointer to a virtual-screen coordinate and scrolls the mouse wheel. Positive scroll_y scrolls up, positive scroll_x scrolls right.")]
  public async Task<string> Scroll(
      [Description("X coordinate in virtual-screen pixels.")]
        int x,
      [Description("Y coordinate in virtual-screen pixels.")]
        int y,
      [Description("Vertical wheel clicks; positive scrolls up, negative scrolls down. Default: 0.")]
        int scroll_y = 0,
      [Description("Horizontal wheel clicks; positive scrolls right, negative scrolls left. Default: 0.")]
        int scroll_x = 0)
  {
    var layout = await _backend.GetDisplayLayoutAsync();
    var point = layout.Clamp(ToPoint(x, y));
    await _backend.ScrollAsync(point, scroll_y, scroll_x);
    return $"Scrolled ({scroll_y} vertical, {scroll_x} horizontal) at ({point.X}, {point.Y}).";
  }

  [McpServerTool(Name = "type_text")]
  [Description("Types literal text at the current keyboard focus. Prefer this for content; use press_key for shortcuts and special keys.")]
  public async Task<string> TypeText(
      [Description("The text to type. Newlines produce Enter presses.")]
        string text)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(text);
    await _backend.TypeTextAsync(text);
    _logger.LogInformation("Typed {Length} characters.", text.Length);
    return $"Typed {text.Length} character(s).";
  }
}
