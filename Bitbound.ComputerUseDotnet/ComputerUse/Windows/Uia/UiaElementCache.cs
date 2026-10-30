using System.Globalization;
using System.Text;

namespace Bitbound.ComputerUseDotnet.ComputerUse.Windows;

/// <summary>Metadata for one UIA element captured during a list_elements walk.</summary>
internal sealed record UiaElementSnapshot(
    string Id,
    string ControlType,
    string Name,
    string AutomationId,
    int X,
    int Y,
    int Width,
    int Height,
    IReadOnlyList<string> Patterns);

/// <summary>
/// Session-scoped handle table mapping element ids (e1, e2, ...) to cached UIA element objects.
/// Ids are never reused. Re-listing a window replaces that window's entries, so ids from an older
/// listing fail loudly instead of acting on a stale element.
/// </summary>
internal sealed class UiaElementCache
{
  private readonly Dictionary<string, CachedElement> _elements = [];

  private int _nextId;

  public int Count => _elements.Count;

  public string Add(nint windowHandle, object element, UiaElementSnapshot snapshotWithoutId)
  {
    var id = $"e{Interlocked.Increment(ref _nextId)}";
    var snapshot = snapshotWithoutId with { Id = id };
    _elements[id] = new CachedElement(windowHandle, element, snapshot);
    return id;
  }

  /// <summary>Drops every entry belonging to a window so its old ids stop resolving.</summary>
  public int RemoveWindow(nint windowHandle)
  {
    var stale = _elements.Where(pair => pair.Value.WindowHandle == windowHandle).Select(pair => pair.Key).ToList();

    foreach (var id in stale)
    {
      _elements.Remove(id);
    }

    return stale.Count;
  }

  public bool TryGet(string id, out CachedElement cached) => _elements.TryGetValue(id, out cached!);

  internal sealed record CachedElement(nint WindowHandle, object Element, UiaElementSnapshot Snapshot);
}

internal enum UiaAction
{
  Invoke,
  Toggle,
  Expand,
  Collapse,
  SetValue,
  Select,
  ScrollIntoView,
}

internal static class UiaActions
{
  private static readonly Dictionary<string, UiaAction> _byName = new(StringComparer.OrdinalIgnoreCase)
  {
    ["invoke"] = UiaAction.Invoke,
    ["toggle"] = UiaAction.Toggle,
    ["expand"] = UiaAction.Expand,
    ["collapse"] = UiaAction.Collapse,
    ["set_value"] = UiaAction.SetValue,
    ["select"] = UiaAction.Select,
    ["scroll_into_view"] = UiaAction.ScrollIntoView,
  };

  public static IReadOnlyList<string> Names => _byName.Keys.ToList();

  /// <summary>Pattern name the action needs, or null when the action needs no pattern.</summary>
  public static string? RequiredPattern(UiaAction action) => action switch
  {
    UiaAction.Invoke => "invoke",
    UiaAction.Toggle => "toggle",
    UiaAction.Expand or UiaAction.Collapse => "expand_collapse",
    UiaAction.SetValue => "value",
    UiaAction.Select => "selection_item",
    UiaAction.ScrollIntoView => "scroll_item",
    _ => null,
  };

  public static bool TryParse(string action, out UiaAction parsed) => _byName.TryGetValue(action.Trim(), out parsed);

  /// <summary>Validates the value parameter against the action, throwing a tool-facing message.</summary>
  public static void ValidateValue(UiaAction action, string? value)
  {
    if (action == UiaAction.SetValue && string.IsNullOrEmpty(value))
    {
      throw new InvalidOperationException("set_value requires the value parameter.");
    }

    if (action != UiaAction.SetValue && !string.IsNullOrEmpty(value))
    {
      throw new InvalidOperationException($"The value parameter is only used by set_value, not '{ action.ToString().ToLowerInvariant()}'.");
    }
  }
}

internal static class UiaFormat
{
  public static string Double(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

  public static string ElementLine(UiaElementSnapshot element) =>
    $"[{element.Id}] {element.ControlType} \"{element.Name}\"" +
    (string.IsNullOrEmpty(element.AutomationId) ? string.Empty : $" automationId={element.AutomationId}") +
    $" rect=({element.X}, {element.Y}) {element.Width}x{element.Height}" +
    (element.Patterns.Count == 0 ? " patterns=none" : $" patterns={string.Join(",", element.Patterns)}");

  public static string ListHeader(string windowDescription, int shown, bool truncated, int depthLimitReached) =>
    $"{windowDescription}: {shown} element(s) listed" +
    (truncated ? " (walk stopped at the element limit; narrow the filters or raise max_elements)." : ".") +
    (depthLimitReached > 0 ? $" {depthLimitReached} element(s) were at or below max_depth and their children were skipped." : string.Empty) +
    " Use an id with perform_action or get_element_state. Ids expire when the window is re-listed.";

  public static string State(
      UiaElementSnapshot element,
      string? enabled,
      string? offscreen,
      string? value,
      string? toggle,
      string? rangeValue,
      string? expandCollapse,
      string? selected)
  {
    var builder = new StringBuilder();
    builder.AppendLine(ElementLine(element));

    if (enabled is not null)
    {
      builder.AppendLine($"Enabled: {enabled}");
    }

    if (offscreen is not null)
    {
      builder.AppendLine($"Offscreen: {offscreen}");
    }

    if (value is not null)
    {
      builder.AppendLine($"Value: {value}");
    }

    if (rangeValue is not null)
    {
      builder.AppendLine($"RangeValue: {rangeValue}");
    }

    if (toggle is not null)
    {
      builder.AppendLine($"Toggle: {toggle}");
    }

    if (expandCollapse is not null)
    {
      builder.AppendLine($"ExpandCollapse: {expandCollapse}");
    }

    if (selected is not null)
    {
      builder.AppendLine($"Selected: {selected}");
    }

    return builder.ToString().TrimEnd();
  }
}
