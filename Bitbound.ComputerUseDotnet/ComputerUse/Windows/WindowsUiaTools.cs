using Bitbound.ComputerUseDotnet.ComputerUse.Windows;
using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;

namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>
/// Windows-only MCP tools that activate UI Automation controls through their control patterns, so
/// agents can click, toggle, and expand without moving the real pointer. Registered only when the
/// server runs on Windows.
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public sealed class WindowsUiaTools(WindowsUiaService uia, ILogger<WindowsUiaTools> logger)
{
  private readonly ILogger<WindowsUiaTools> _logger = logger;
  private readonly WindowsUiaService _uia = uia;

  [McpServerTool(Name = "get_element_state")]
  [Description(
    "Reads one UIA element by its id from list_elements (Windows only) without a screenshot: " +
    "name, control type, enabled, offscreen, Value/RangeValue, Toggle state, ExpandCollapse state, and " +
    "selection. Use it to verify an action or read control state. Fails if the id is unknown or stale; " +
    "re-run list_elements.")]
  public async Task<string> GetElementState(
      [Description("Element id from list_elements (e.g. e3).")]
        string element_id) =>
    await ComputerUseTools.RunToolAsync("get_element_state", _logger, () =>
      _uia.GetElementState(element_id));

  [McpServerTool(Name = "list_elements")]
  [Description(
    "Walks the UI Automation (UIA) tree of one desktop window (Windows only) and lists actionable " +
    "elements with stable ids (e1, e2, ...) for use with perform_action and get_element_state. " +
    "Each line gives id, control type, name, automation id, bounding rect in physical pixels, and " +
    "supported patterns (invoke, toggle, expand_collapse, value, range_value, selection_item, scroll_item). " +
    "Ids expire when the same window is listed again, and acting on a stale id fails; re-run this tool then. " +
    "Only UIA-exposed controls appear. Canvas, game, and some Electron/Avalonia surfaces expose nothing; use " +
    "capture_window + click for those. Prefer these tools over pixel clicks when elements are listed: " +
    "pattern activation is exact and does not move the pointer.")]
  public async Task<string> ListElements(
      [Description("Window handle (0x-prefixed hex or decimal) or a title substring, as in list_windows.")]
        string window,
      [Description("Maximum tree depth to walk. The window root is depth 0. Clamped to 1..100, default 25.")]
        int max_depth = 25,
      [Description("Maximum number of elements to return. Clamped to 1..1000, default 200.")]
        int max_elements = 200,
      [Description("Optional case-insensitive substring filter on the localized control type (e.g. 'button').")]
        string? control_type = null,
      [Description("Optional case-insensitive substring filter on the element name.")]
        string? name = null) =>
    await ComputerUseTools.RunToolAsync("list_elements", _logger, async () =>
    {
      var depth = Math.Clamp(max_depth, 1, 100);
      var count = Math.Clamp(max_elements, 1, 1000);

      var result = await _uia.ListElements(window, depth, count, control_type, name);

      if (result.Elements.Count == 0)
      {
        return $"{result.Window.Title}: no UIA elements matched. The window may expose no UIA tree (custom-drawn or GPU-composited UI); use capture_window + click instead. " +
          "Widen max_depth or drop the control_type/name filters if the tree is deeper than expected.";
      }

      var lines = new List<string>
      {
        UiaFormat.ListHeader(
          $"Window {WindowsWindowService.FormatHandle(result.Window.Handle)} \"{result.Window.Title}\"",
          result.Elements.Count,
          result.Truncated,
          result.DepthSkipped),
      };

      lines.AddRange(result.Elements.Select(UiaFormat.ElementLine));
      return string.Join(Environment.NewLine, lines);
    });

  [McpServerTool(Name = "perform_action")]
  [Description(
    "Activates a UIA element by its id from list_elements (Windows only) through its control pattern: " +
    "invoke (button press), toggle (checkbox), expand / collapse (tree, combo, expander), set_value " +
    "(Value pattern, needs the value parameter), select (SelectionItem), scroll_into_view. The real " +
    "pointer is not moved and no input is synthesized. The app emulates the action, so hover states and " +
    "custom-drawn controls will not react; fall back to click with desktop fractions for those. " +
    "Fails if the id is unknown or stale; re-run list_elements.")]
  public async Task<string> PerformAction(
      [Description("Element id from list_elements (e.g. e3).")]
        string element_id,
      [Description("Action to perform: invoke, toggle, expand, collapse, set_value, select, scroll_into_view.")]
        string action,
      [Description("Required by set_value; must be omitted for other actions.")]
        string? value = null) =>
    await ComputerUseTools.RunToolAsync("perform_action", _logger, () =>
      _uia.PerformAction(element_id, action, value));
}
