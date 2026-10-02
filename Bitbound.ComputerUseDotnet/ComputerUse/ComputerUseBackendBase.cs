namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>
/// Base class that implements composite pointer actions (click, double-click, drag) on top of
/// the platform primitives <see cref="MovePointerAsync"/> and <see cref="SetPointerButtonAsync"/>.
/// </summary>
public abstract class ComputerUseBackendBase : IComputerUseBackend
{
  private static readonly TimeSpan ButtonDelay = TimeSpan.FromMilliseconds(12);
  private static readonly TimeSpan MoveSettleDelay = TimeSpan.FromMilliseconds(25);

  private bool _disposed;

  public abstract string BackendName { get; }

  public abstract DesktopEnvironmentType EnvironmentType { get; }

  public abstract Task<DisplayLayout> GetDisplayLayoutAsync(CancellationToken cancellationToken = default);

  public abstract Task<SKBitmap> CaptureVirtualScreenAsync(CancellationToken cancellationToken = default);

  public virtual async Task ClickAsync(
    ScreenPoint point,
    MouseButton button,
    int clickCount,
    CancellationToken cancellationToken = default)
  {
    await MovePointerAsync(point, cancellationToken);
    await Task.Delay(MoveSettleDelay, cancellationToken);

    for (var i = 0; i < Math.Max(1, clickCount); i++)
    {
      await SetPointerButtonAsync(button, true, cancellationToken);
      await Task.Delay(ButtonDelay, cancellationToken);
      await SetPointerButtonAsync(button, false, cancellationToken);
      await Task.Delay(ButtonDelay, cancellationToken);
    }
  }

  public virtual async Task DragAsync(
    ScreenPoint start,
    ScreenPoint end,
    MouseButton button,
    int steps,
    CancellationToken cancellationToken = default)
  {
    await MovePointerAsync(start, cancellationToken);
    await Task.Delay(MoveSettleDelay, cancellationToken);
    await SetPointerButtonAsync(button, true, cancellationToken);
    await Task.Delay(ButtonDelay, cancellationToken);

    var stepCount = Math.Max(1, steps);

    for (var i = 1; i <= stepCount; i++)
    {
      var t = (double)i / stepCount;
      var x = (int)Math.Round(start.X + ((end.X - start.X) * t));
      var y = (int)Math.Round(start.Y + ((end.Y - start.Y) * t));
      await MovePointerAsync(new ScreenPoint(x, y), cancellationToken);
      await Task.Delay(MoveSettleDelay, cancellationToken);
    }

    await SetPointerButtonAsync(button, false, cancellationToken);
  }

  public abstract Task MovePointerAsync(ScreenPoint point, CancellationToken cancellationToken = default);

  public abstract Task SetPointerButtonAsync(MouseButton button, bool pressed, CancellationToken cancellationToken = default);

  public abstract Task ScrollAsync(ScreenPoint point, int verticalClicks, int horizontalClicks, CancellationToken cancellationToken = default);

  public abstract Task TypeTextAsync(string text, CancellationToken cancellationToken = default);

  public abstract Task PressChordAsync(KeyChord chord, CancellationToken cancellationToken = default);

  public abstract Task<ScreenPoint?> GetCursorPositionAsync(CancellationToken cancellationToken = default);

  public abstract Task<PermissionStatus> CheckPermissionsAsync(CancellationToken cancellationToken = default);

  public abstract Task<PermissionStatus> RequestPermissionsAsync(CancellationToken cancellationToken = default);

  public void Dispose()
  {
    if (_disposed)
    {
      return;
    }

    _disposed = true;
    DisposeCore();
    GC.SuppressFinalize(this);
  }

  protected abstract void DisposeCore();
}
