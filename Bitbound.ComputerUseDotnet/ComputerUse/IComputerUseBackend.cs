namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>
/// Platform implementation for screen capture, pointer/keyboard simulation, and
/// permission management. All coordinates are in virtual-screen (normalized) space:
/// logical pixels of the union of all displays, origin at the union's top-left corner.
/// </summary>
public interface IComputerUseBackend : IDisposable
{
  /// <summary>Short name of the active backend, e.g. "Windows (GDI + SendInput)".</summary>
  string BackendName { get; }

  /// <summary>The kind of desktop session in use.</summary>
  DesktopEnvironmentType EnvironmentType { get; }

  /// <summary>Enumerates displays and computes the virtual-screen layout.</summary>
  Task<DisplayLayout> GetDisplayLayoutAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Captures the full virtual screen as an image whose pixels correspond 1:1 with
  /// virtual-screen coordinates (physical resolution downscaled to logical size when needed).
  /// </summary>
  Task<SKBitmap> CaptureVirtualScreenAsync(CancellationToken cancellationToken = default);

  /// <summary>Clicks a pointer button at the given virtual-screen point.</summary>
  Task ClickAsync(ScreenPoint point, MouseButton button, int clickCount, CancellationToken cancellationToken = default);

  /// <summary>Drags with a button held from one virtual-screen point to another.</summary>
  Task DragAsync(ScreenPoint start, ScreenPoint end, MouseButton button, int steps, CancellationToken cancellationToken = default);

  /// <summary>Moves the pointer to the given virtual-screen point.</summary>
  Task MovePointerAsync(ScreenPoint point, CancellationToken cancellationToken = default);

  /// <summary>Presses or releases a pointer button at the current pointer position.</summary>
  Task SetPointerButtonAsync(MouseButton button, bool pressed, CancellationToken cancellationToken = default);

  /// <summary>Scrolls the wheel at the given point. Positive vertical clicks scroll up.</summary>
  Task ScrollAsync(ScreenPoint point, int verticalClicks, int horizontalClicks, CancellationToken cancellationToken = default);

  /// <summary>Types literal text (unicode where supported).</summary>
  Task TypeTextAsync(string text, CancellationToken cancellationToken = default);

  /// <summary>Presses a key chord (modifiers + one target key).</summary>
  Task PressChordAsync(KeyChord chord, CancellationToken cancellationToken = default);

  /// <summary>Current pointer position in virtual-screen space, or null when unsupported.</summary>
  Task<ScreenPoint?> GetCursorPositionAsync(CancellationToken cancellationToken = default);

  /// <summary>Reports screen-capture and input-simulation permission state without prompting.</summary>
  Task<PermissionStatus> CheckPermissionsAsync(CancellationToken cancellationToken = default);

  /// <summary>Prompts for any permissions not yet granted (may block on user interaction).</summary>
  Task<PermissionStatus> RequestPermissionsAsync(CancellationToken cancellationToken = default);
}
