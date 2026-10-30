using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Bitbound.ComputerUseDotnet.ComputerUse.Windows;

/// <summary>
/// Drives UI Automation control patterns (Invoke, Toggle, ExpandCollapse, Value, SelectionItem,
/// ScrollItem) against cached elements so agents can act on controls without moving the pointer.
/// Pattern activation is app-emulated, not a real mouse click; custom-drawn surfaces ignore it.
/// All UIA calls run on thread-pool threads (MTA, as UIA requires) behind an overall timeout.
/// A timed-out call is abandoned, not aborted; its COM worker thread stays blocked until the
/// provider returns, which is acceptable for a first iteration on hung-provider protection.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsUiaService
{
  private static readonly (string Name, int PatternId)[] SupportedPatterns =
  [
    ("invoke", Uia.PatternInvoke),
    ("toggle", Uia.PatternToggle),
    ("expand_collapse", Uia.PatternExpandCollapse),
    ("value", Uia.PatternValue),
    ("range_value", Uia.PatternRangeValue),
    ("selection_item", Uia.PatternSelectionItem),
    ("scroll_item", Uia.PatternScrollItem),
  ];
  private static readonly TimeSpan UiaTimeout = TimeSpan.FromSeconds(10);

  private readonly UiaElementCache _cache = new();
  private readonly object _sync = new();
  private readonly WindowsWindowService _windows;

  private IUIAutomation? _uia;

  public WindowsUiaService(WindowsWindowService windows)
  {
    _windows = windows;
  }

  public async Task<string> GetElementState(string elementId)
  {
    var cached = Lookup(elementId);

    return await Run(() =>
    {
      var element = AsElement(cached);

      // One property read first; if the element is dead every read would throw and this gives the
      // re-run-list_elements message once instead of silently omitting every line.
      element.GetCurrentPropertyValue(Uia.PropertyName, out _);

      var enabled = TryGetBoolProperty(element, Uia.PropertyIsEnabled);
      var offscreen = TryGetBoolProperty(element, Uia.PropertyIsOffscreen);

      string? value = null;
      if (TryGetPattern(element, "value") is IUIAutomationValuePattern valuePattern)
      {
        value = $"\"{valuePattern.CurrentValue}\" readonly={valuePattern.CurrentIsReadOnly != 0}";
      }

      string? rangeValue = null;
      if (TryGetPattern(element, "range_value") is IUIAutomationRangeValuePattern rangePattern)
      {
        rangeValue = $"{UiaFormat.Double(rangePattern.CurrentValue)} (min {UiaFormat.Double(rangePattern.CurrentMinimum)}, max {UiaFormat.Double(rangePattern.CurrentMaximum)}, readonly={rangePattern.CurrentIsReadOnly != 0})";
      }

      string? toggle = null;
      if (TryGetPattern(element, "toggle") is IUIAutomationTogglePattern togglePattern)
      {
        toggle = togglePattern.CurrentToggleState.ToString();
      }

      string? expandCollapse = null;
      if (TryGetPattern(element, "expand_collapse") is IUIAutomationExpandCollapsePattern expandPattern)
      {
        expandCollapse = expandPattern.CurrentExpandCollapseState.ToString();
      }

      string? selected = null;
      if (TryGetPattern(element, "selection_item") is IUIAutomationSelectionItemPattern selectionPattern)
      {
        selected = selectionPattern.CurrentIsSelected != 0 ? "yes" : "no";
      }

      return UiaFormat.State(cached.Snapshot, enabled, offscreen, value, toggle, rangeValue, expandCollapse, selected);
    });
  }

  public async Task<string> PerformAction(string elementId, string action, string? value)
  {
    if (!UiaActions.TryParse(action, out var parsed))
    {
      throw new InvalidOperationException($"Unknown action '{action}'. Valid actions: {string.Join(", ", UiaActions.Names)}.");
    }

    UiaActions.ValidateValue(parsed, value);
    var setValue = parsed == UiaAction.SetValue
      ? value ?? throw new InvalidOperationException("set_value requires the value parameter.")
      : null;
    var cached = Lookup(elementId);

    return await Run(() =>
    {
      var element = AsElement(cached);
      object Require(string patternName) =>
        TryGetPattern(element, patternName)
        ?? throw new InvalidOperationException(
          $"Element {elementId} does not support the {patternName} pattern. Re-run list_elements to check its current patterns, or use click for custom-drawn controls.");

      switch (parsed)
      {
        case UiaAction.Invoke:
          ((IUIAutomationInvokePattern)Require("invoke")).Invoke();
          break;
        case UiaAction.Toggle:
          ((IUIAutomationTogglePattern)Require("toggle")).Toggle();
          break;
        case UiaAction.Expand:
          ((IUIAutomationExpandCollapsePattern)Require("expand_collapse")).Expand();
          break;
        case UiaAction.Collapse:
          ((IUIAutomationExpandCollapsePattern)Require("expand_collapse")).Collapse();
          break;
        case UiaAction.SetValue:
          ((IUIAutomationValuePattern)Require("value")).SetValue(setValue ?? throw new InvalidOperationException("set_value requires the value parameter."));
          break;
        case UiaAction.Select:
          ((IUIAutomationSelectionItemPattern)Require("selection_item")).Select();
          break;
        case UiaAction.ScrollIntoView:
          ((IUIAutomationScrollItemPattern)Require("scroll_item")).ScrollIntoView();
          break;
      }

      var description = cached.Snapshot.Name.Length > 0 ? $"\"{cached.Snapshot.Name}\"" : cached.Snapshot.ControlType;

      return $"{parsed.ToSentence()} {cached.Snapshot.ControlType} {description} ({elementId}) succeeded. The app emulated the action; hover-only or custom-drawn UI may not have reacted. Re-list or re-read state if needed.";
    });
  }

  internal async Task<ListResult> ListElements(string window, int maxDepth, int maxElements, string? controlType, string? name)
  {
    return await Run(() =>
    {
      var info = _windows.Resolve(window);
      var root = GetAutomation().ElementFromHandleRoot(info.Handle);
      var walker = GetAutomation().ControlViewWalker();

      var matched = new List<UiaElementSnapshot>();
      var truncated = false;
      var depthSkipped = 0;

      // The window's old ids stop resolving the moment it is re-listed.
      _cache.RemoveWindow(info.Handle);

      var stack = new Stack<(IUIAutomationElement Element, int Depth)>();
      stack.Push((root, 0));

      while (stack.Count > 0)
      {
        var (element, depth) = stack.Pop();

        var snapshot = Describe(element);

        var matches =
          (controlType is null || snapshot.ControlType.Contains(controlType, StringComparison.OrdinalIgnoreCase)) &&
          (name is null || snapshot.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        if (matches)
        {
          var id = _cache.Add(info.Handle, element, snapshot);
          matched.Add(snapshot with { Id = id });
        }

        if (depth >= maxDepth)
        {
          depthSkipped++;
          continue;
        }

        // Push children in reverse so the pop order is document order, depth first.
        var children = new List<IUIAutomationElement>();

        for (var child = walker.GetFirstChild(element); child is not null; child = walker.GetNextSibling(child))
        {
          children.Add(child);
        }

        for (var i = children.Count - 1; i >= 0; i--)
        {
          stack.Push((children[i], depth + 1));
        }

        if (matched.Count >= maxElements)
        {
          truncated = true;
          break;
        }
      }

      return new ListResult(info, matched, truncated, depthSkipped);
    });
  }

  private static IUIAutomationElement AsElement(UiaElementCache.CachedElement cached) =>
    cached.Element as IUIAutomationElement
      ?? throw new InvalidOperationException($"Cached element {cached.Snapshot.Id} is corrupt. Re-run list_elements.");

  private static UiaElementSnapshot Describe(IUIAutomationElement element)
  {
    var controlType = TryGetStringProperty(element, Uia.PropertyLocalizedControlType) ?? "custom";
    var name = TryGetStringProperty(element, Uia.PropertyName) ?? string.Empty;
    var automationId = TryGetStringProperty(element, Uia.PropertyAutomationId) ?? string.Empty;

    var x = 0;
    var y = 0;
    var width = 0;
    var height = 0;

    element.GetCurrentPropertyValue(Uia.PropertyBoundingRectangle, out var rectValue);

    if (ToDoubles(rectValue) is { Length: 4 } rect)
    {
      x = (int)Math.Round(rect[0]);
      y = (int)Math.Round(rect[1]);
      width = (int)Math.Round(rect[2]);
      height = (int)Math.Round(rect[3]);
    }

    var patterns = new List<string>();

    foreach (var (patternName, patternId) in SupportedPatterns)
    {
      if (TryGetPattern(element, patternId) is not null)
      {
        patterns.Add(patternName);
      }
    }

    return new UiaElementSnapshot(string.Empty, controlType, name, automationId, x, y, width, height, patterns);
  }

  private static async Task<T> Run<T>(Func<T> work)
  {
    var task = Task.Run(work);
    var finished = await Task.WhenAny(task, Task.Delay(UiaTimeout));

    if (finished != task)
    {
      throw new InvalidOperationException(
        $"The UI Automation provider did not answer within {(int)UiaTimeout.TotalSeconds} seconds. " +
        "The app may be busy or its UIA provider hung. Retry, or fall back to the pixel tools (take_screenshot + click).");
    }

    try
    {
      return await task;
    }
    catch (COMException exception)
    {
      throw new InvalidOperationException(
        $"The UIA element is no longer valid (0x{exception.HResult:X8}). The app likely rebuilt its tree. Re-run list_elements.",
        exception);
    }
  }

  private static double[]? ToDoubles(object? value) => value switch
  {
    double[] array => array,
    object[] array => array.OfType<double>().ToArray(),
    _ => null,
  };

  private static string? TryGetBoolProperty(IUIAutomationElement element, int propertyId)
  {
    element.GetCurrentPropertyValue(propertyId, out var value);

    return value switch
    {
      bool flag => flag ? "yes" : "no",
      int number => number != 0 ? "yes" : "no",
      _ => null,
    };
  }

  private static object? TryGetPattern(IUIAutomationElement element, string patternName)
  {
    var patternId = SupportedPatterns.First(pair => pair.Name == patternName).PatternId;
    return TryGetPattern(element, patternId);
  }

  private static object? TryGetPattern(IUIAutomationElement element, int patternId)
  {
    try
    {
      element.GetCurrentPattern(patternId, out var patternObject);
      return patternObject;
    }
    catch (COMException)
    {
      // Providers report unsupported patterns inconsistently (null or an error).
      return null;
    }
  }

  private static string? TryGetStringProperty(IUIAutomationElement element, int propertyId)
  {
    element.GetCurrentPropertyValue(propertyId, out var value);
    return value as string;
  }

  private IUIAutomation GetAutomation()
  {
    lock (_sync)
    {
      if (_uia is not null)
      {
        return _uia;
      }

      var type = Type.GetTypeFromCLSID(Uia.AutomationClsid)
        ?? throw new InvalidOperationException("UIAutomationCore is not registered on this machine.");

      // The server process is any-CPU and thread-pool threads are MTA, which satisfies the
      // RO_INIT_MULTITHREADED apartment UIA clients require.
      var instance = Activator.CreateInstance(type)
        ?? throw new InvalidOperationException("Failed to create the UIAutomation COM object.");

      _uia = (IUIAutomation)instance;
      return _uia;
    }
  }

  private UiaElementCache.CachedElement Lookup(string elementId)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(elementId);

    if (!_cache.TryGet(elementId.Trim(), out var cached))
    {
      throw new InvalidOperationException($"Unknown element id '{elementId}'. Ids come from list_elements and expire when the window is re-listed. Re-run list_elements.");
    }

    return cached;
  }

  internal sealed record ListResult(WindowInfo Window, IReadOnlyList<UiaElementSnapshot> Elements, bool Truncated, int DepthSkipped);
}

internal static class UiaWalkerExtensions
{
  public static IUIAutomationTreeWalker ControlViewWalker(this IUIAutomation automation)
  {
    automation.get_ControlViewWalker(out var walker);
    return walker ?? throw new InvalidOperationException("UIAutomation returned no control view walker.");
  }

  public static IUIAutomationElement ElementFromHandleRoot(this IUIAutomation automation, nint hwnd)
  {
    automation.ElementFromHandle(hwnd, out var element);
    return element ?? throw new InvalidOperationException($"ElementFromHandle returned no element for window {WindowsWindowService.FormatHandle(hwnd)}.");
  }

  public static IUIAutomationElement? GetFirstChild(this IUIAutomationTreeWalker walker, IUIAutomationElement element)
  {
    walker.GetFirstChildElement(element, out var first);
    return first;
  }

  public static IUIAutomationElement? GetNextSibling(this IUIAutomationTreeWalker walker, IUIAutomationElement element)
  {
    walker.GetNextSiblingElement(element, out var next);
    return next;
  }
}

internal static class StringActionExtensions
{
  public static string ToSentence(this UiaAction action) => action switch
  {
    UiaAction.Invoke => "Invoked",
    UiaAction.Toggle => "Toggled",
    UiaAction.Expand => "Expanded",
    UiaAction.Collapse => "Collapsed",
    UiaAction.SetValue => "Set value on",
    UiaAction.Select => "Selected",
    UiaAction.ScrollIntoView => "Scrolled into view",
    _ => action.ToString(),
  };
}
