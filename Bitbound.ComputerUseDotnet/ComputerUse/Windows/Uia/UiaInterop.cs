using System.Runtime.InteropServices;

// COM interop members must stay in UIAutomationClient.h vtable order, which outranks member ordering.
#pragma warning disable BB0001

namespace Bitbound.ComputerUseDotnet.ComputerUse.Windows;

/// <summary>
/// Hand-written UI Automation client interop for UIAutomationCore. Interface member order is the
/// vtable order from UIAutomationClient.h and must not be reordered. Slots that are never called are
/// padded with placeholder methods. Trailing slots beyond the last called method are omitted, which
/// is safe because the CLR binds by declaration index and undeclared slots are never invoked.
/// </summary>
internal static class Uia
{
  public static readonly Guid AutomationClsid = new("FF48DBA4-60EF-4201-AA87-54103EEF594E");

  public const int PropertyBoundingRectangle = 30001;
  public const int PropertyControlType = 30003;
  public const int PropertyLocalizedControlType = 30004;
  public const int PropertyName = 30005;
  public const int PropertyIsEnabled = 30010;
  public const int PropertyAutomationId = 30011;
  public const int PropertyIsOffscreen = 30022;

  public const int PatternInvoke = 10000;
  public const int PatternToggle = 10015;
  public const int PatternExpandCollapse = 10003;
  public const int PatternValue = 10002;
  public const int PatternRangeValue = 10004;
  public const int PatternSelectionItem = 10010;
  public const int PatternScrollItem = 10017;
}

[ComImport, Guid("30CBE57D-D9D0-452A-AB13-7AC5AC4825EE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomation
{
  void _CompareElements();
  void _CompareRuntimeIds();
  void _GetRootElement();

  void ElementFromHandle(nint hwnd, [MarshalAs(UnmanagedType.Interface)] out IUIAutomationElement element);

  void _ElementFromPoint();
  void _GetFocusedElement();
  void _GetRootElementBuildCache();
  void _ElementFromHandleBuildCache();
  void _ElementFromPointBuildCache();
  void _GetFocusedElementBuildCache();
  void _CreateTreeWalker();

  void get_ControlViewWalker([MarshalAs(UnmanagedType.Interface)] out IUIAutomationTreeWalker walker);
}

[ComImport, Guid("4042C624-389C-4AFC-A630-9DF854A541FC"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationTreeWalker
{
  void _GetParentElement();

  void GetFirstChildElement(
    [MarshalAs(UnmanagedType.Interface)] IUIAutomationElement element,
    [MarshalAs(UnmanagedType.Interface)] out IUIAutomationElement? first);

  void _GetLastChildElement();

  void GetNextSiblingElement(
    [MarshalAs(UnmanagedType.Interface)] IUIAutomationElement element,
    [MarshalAs(UnmanagedType.Interface)] out IUIAutomationElement? next);
}

[ComImport, Guid("D22108AA-8AC5-49A5-837B-37BBB3D7591E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationElement
{
  void _SetFocus();
  void _GetRuntimeId();
  void _FindFirst();
  void _FindAll();
  void _FindFirstBuildCache();
  void _FindAllBuildCache();
  void _BuildUpdatedCache();

  void GetCurrentPropertyValue(
    int propertyId,
    [MarshalAs(UnmanagedType.Struct)] out object? value);

  void _GetCurrentPropertyValueEx();
  void _GetCachedPropertyValue();
  void _GetCachedPropertyValueEx();
  void _GetCurrentPatternAs();
  void _GetCachedPatternAs();

  void GetCurrentPattern(
    int patternId,
    [MarshalAs(UnmanagedType.IUnknown)] out object? patternObject);
}

[ComImport, Guid("FB377FBE-8EA6-46D5-9C73-6499642D3059"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationInvokePattern
{
  void Invoke();
}

[ComImport, Guid("94CF8058-9B8D-4AB9-8BFD-4CD0A33C8C70"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationTogglePattern
{
  void Toggle();

  UiaToggleState CurrentToggleState { get; }
}

[ComImport, Guid("619BE086-1F4E-4EE4-BAFA-210128738730"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationExpandCollapsePattern
{
  void Expand();
  void Collapse();

  UiaExpandCollapseState CurrentExpandCollapseState { get; }
}

[ComImport, Guid("A94CD8B1-0844-4CD6-9D2D-640537AB39E9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationValuePattern
{
  void SetValue([MarshalAs(UnmanagedType.BStr)] string value);

  string CurrentValue { [return: MarshalAs(UnmanagedType.BStr)] get; }

  int CurrentIsReadOnly { get; }
}

[ComImport, Guid("59213F4F-7346-49E5-B120-80555987A148"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationRangeValuePattern
{
  void SetValue(double value);

  double CurrentValue { get; }

  int CurrentIsReadOnly { get; }

  double CurrentMaximum { get; }

  double CurrentMinimum { get; }
}

[ComImport, Guid("A8EFA66A-0FDA-421A-9194-38021F3578EA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationSelectionItemPattern
{
  void Select();
  void _AddToSelection();
  void _RemoveFromSelection();

  int CurrentIsSelected { get; }
}

[ComImport, Guid("B488300F-D015-4F19-9C29-BB595E3645EF"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationScrollItemPattern
{
  void ScrollIntoView();
}

internal enum UiaToggleState
{
  Off = 0,
  On = 1,
  Indeterminate = 2,
}

internal enum UiaExpandCollapseState
{
  Collapsed = 0,
  Expanded = 1,
  PartiallyExpanded = 2,
  LeafNode = 3,
}
