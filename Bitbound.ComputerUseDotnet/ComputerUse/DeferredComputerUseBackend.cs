using Bitbound.SystemAbstractions.FileSystem;
using Microsoft.Extensions.Logging;

namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>
/// Defers backend creation until the first tool call so the MCP server can start and
/// list its tools even when no usable desktop session is present yet.
/// </summary>
public sealed class DeferredComputerUseBackend(IFileSystem fileSystem, ILoggerFactory loggerFactory) : IComputerUseBackend
{
  private readonly SemaphoreSlim _gate = new(1, 1);
  private readonly ILogger _logger = loggerFactory.CreateLogger<DeferredComputerUseBackend>();
  private IComputerUseBackend? _backend;
  private string? _startupError;

  public string BackendName => Ensure().BackendName;

  public DesktopEnvironmentType EnvironmentType => Ensure().EnvironmentType;

  public async Task<DisplayLayout> GetDisplayLayoutAsync(CancellationToken cancellationToken = default) =>
    await Ensure().GetDisplayLayoutAsync(cancellationToken);

  public async Task<SKBitmap> CaptureVirtualScreenAsync(CancellationToken cancellationToken = default) =>
    await Ensure().CaptureVirtualScreenAsync(cancellationToken);

  public async Task ClickAsync(ScreenPoint point, MouseButton button, int clickCount, CancellationToken cancellationToken = default) =>
    await Ensure().ClickAsync(point, button, clickCount, cancellationToken);

  public async Task DragAsync(ScreenPoint start, ScreenPoint end, MouseButton button, int steps, CancellationToken cancellationToken = default) =>
    await Ensure().DragAsync(start, end, button, steps, cancellationToken);

  public async Task MovePointerAsync(ScreenPoint point, CancellationToken cancellationToken = default) =>
    await Ensure().MovePointerAsync(point, cancellationToken);

  public async Task SetPointerButtonAsync(MouseButton button, bool pressed, CancellationToken cancellationToken = default) =>
    await Ensure().SetPointerButtonAsync(button, pressed, cancellationToken);

  public async Task ScrollAsync(ScreenPoint point, int verticalClicks, int horizontalClicks, CancellationToken cancellationToken = default) =>
    await Ensure().ScrollAsync(point, verticalClicks, horizontalClicks, cancellationToken);

  public async Task TypeTextAsync(string text, CancellationToken cancellationToken = default) =>
    await Ensure().TypeTextAsync(text, cancellationToken);

  public async Task PressChordAsync(KeyChord chord, CancellationToken cancellationToken = default) =>
    await Ensure().PressChordAsync(chord, cancellationToken);

  public async Task<ScreenPoint?> GetCursorPositionAsync(CancellationToken cancellationToken = default) =>
    await Ensure().GetCursorPositionAsync(cancellationToken);

  public async Task<PermissionStatus> CheckPermissionsAsync(CancellationToken cancellationToken = default) =>
    await Ensure().CheckPermissionsAsync(cancellationToken);

  public async Task<PermissionStatus> RequestPermissionsAsync(CancellationToken cancellationToken = default) =>
    await Ensure().RequestPermissionsAsync(cancellationToken);

  public void Dispose()
  {
    _backend?.Dispose();
    _gate.Dispose();
    GC.SuppressFinalize(this);
  }

  private IComputerUseBackend Ensure()
  {
    if (_backend is not null)
    {
      return _backend;
    }

    _gate.Wait();

    try
    {
      if (_backend is not null)
      {
        return _backend;
      }

      if (_startupError is not null)
      {
        throw new InvalidOperationException(_startupError);
      }

      try
      {
        _backend = ComputerUseBackendFactory.Create(fileSystem, loggerFactory);
      }
      catch (Exception ex)
      {
        _startupError = $"Unable to initialize the computer-use backend for this session: {ex.Message}";
        _logger.LogWarning(ex, "Computer-use backend initialization failed.");
        throw new InvalidOperationException(_startupError, ex);
      }

      _logger.LogInformation("Initialized computer-use backend '{BackendName}'.", _backend.BackendName);
      return _backend;
    }
    finally
    {
      _gate.Release();
    }
  }
}
