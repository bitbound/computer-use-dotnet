using Bitbound.ComputerUseDotnet.ComputerUse;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using SkiaSharp;

namespace Bitbound.ComputerUseDotnet.Tests;

/// <summary>Tests for the tool error wrapper and the log-tail helper.</summary>
public sealed class ComputerUseToolsTests
{
  [Fact]
  public async Task CheckPermissions_WrapsThrownExceptionInMcpException()
  {
    var tools = new ComputerUseTools(new ThrowingBackend("permission boom"), NullLogger<ComputerUseTools>.Instance);

    var ex = await Assert.ThrowsAsync<McpException>(() => tools.CheckPermissions());

    Assert.Contains("check_permissions failed", ex.Message);
    Assert.Contains("permission boom", ex.Message);
    Assert.IsType<InvalidOperationException>(ex.InnerException);
  }

  [Fact]
  public async Task CheckPermissions_ThrowsMcpExceptionEvenWhenLoggerThrows()
  {
    var tools = new ComputerUseTools(new ThrowingBackend("boom"), new ThrowingLogger());

    var ex = await Assert.ThrowsAsync<McpException>(() => tools.CheckPermissions());

    Assert.Contains("boom", ex.Message);
  }

  [Fact]
  public async Task TailLog_ReturnsLastLines()
  {
    var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".log");
    await File.WriteAllLinesAsync(path, new[] { "line 1", "line 2", "line 3", "line 4" }, TestContext.Current.CancellationToken);

    try
    {
      var result = await ComputerUseTools.TailLogAsync(path, 2, null);
      Assert.Contains("line 3", result);
      Assert.Contains("line 4", result);
      Assert.DoesNotContain("line 1", result);
    }
    finally
    {
      File.Delete(path);
    }
  }

  [Fact]
  public async Task TailLog_FiltersBySubstringCaseInsensitively()
  {
    var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".log");
    await File.WriteAllLinesAsync(path, new[] { "alpha", "beta", "ALPHA again", "gamma" }, TestContext.Current.CancellationToken);

    try
    {
      var result = await ComputerUseTools.TailLogAsync(path, 100, "alpha");
      Assert.Contains("alpha", result);
      Assert.Contains("ALPHA again", result);
      Assert.DoesNotContain("beta", result);
    }
    finally
    {
      File.Delete(path);
    }
  }

  [Fact]
  public async Task TailLog_ReportsMissingFile()
  {
    var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".log");
    var result = await ComputerUseTools.TailLogAsync(path, 100, null);
    Assert.Contains("No log file", result);
  }

  private sealed class ThrowingBackend(string message) : IComputerUseBackend
  {
    public string BackendName => "throwing";
    public DesktopEnvironmentType EnvironmentType => DesktopEnvironmentType.Windows;
    public Task<SKBitmap> CaptureVirtualScreenAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException(message);
    public Task<PermissionStatus> CheckPermissionsAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException(message);
    public Task ClickAsync(ScreenPoint point, MouseButton button, int clickCount, CancellationToken cancellationToken = default) => throw new InvalidOperationException(message);
    public Task DragAsync(ScreenPoint start, ScreenPoint end, MouseButton button, int steps, CancellationToken cancellationToken = default) => throw new InvalidOperationException(message);
    public Task<ScreenPoint?> GetCursorPositionAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException(message);
    public Task<DisplayLayout> GetDisplayLayoutAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException(message);
    public Task MovePointerAsync(ScreenPoint point, CancellationToken cancellationToken = default) => throw new InvalidOperationException(message);
    public Task PressChordAsync(KeyChord chord, CancellationToken cancellationToken = default) => throw new InvalidOperationException(message);
    public Task<PermissionStatus> RequestPermissionsAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException(message);
    public Task ScrollAsync(ScreenPoint point, int verticalClicks, int horizontalClicks, CancellationToken cancellationToken = default) => throw new InvalidOperationException(message);
    public Task SetPointerButtonAsync(MouseButton button, bool pressed, CancellationToken cancellationToken = default) => throw new InvalidOperationException(message);
    public Task TypeTextAsync(string text, CancellationToken cancellationToken = default) => throw new InvalidOperationException(message);
    public void Dispose() { }
  }

  private sealed class ThrowingLogger : ILogger<ComputerUseTools>
  {
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => throw new IOException("logger boom");
  }
}
