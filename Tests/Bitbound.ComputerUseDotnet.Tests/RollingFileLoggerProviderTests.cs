using Bitbound.ComputerUseDotnet.Logging;
using Microsoft.Extensions.Logging;

namespace Bitbound.ComputerUseDotnet.Tests;

public sealed class RollingFileLoggerProviderTests : IDisposable
{
  private readonly string _directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

  [Fact]
  public void Log_WritesMessageAndExceptionToFile()
  {
    using var provider = new RollingFileLoggerProvider(_directory);
    var logger = provider.CreateLogger("TestCategory");
    var exception = new InvalidOperationException("useful failure detail");

    logger.LogError(exception, "Tool call failed for {ToolName}", "press_key");

    var contents = File.ReadAllText(Path.Combine(_directory, "computer-use-dotnet.log"));
    Assert.Contains("TestCategory", contents);
    Assert.Contains("Tool call failed for press_key", contents);
    Assert.Contains("useful failure detail", contents);
  }

  [Fact]
  public void Log_RotatesWhenFileExceedsLimit()
  {
    using var provider = new RollingFileLoggerProvider(_directory, maxFileSizeBytes: 1, retainedFileCount: 2);
    var logger = provider.CreateLogger("TestCategory");

    logger.LogInformation("First entry");
    logger.LogInformation("Second entry");

    Assert.True(File.Exists(Path.Combine(_directory, "computer-use-dotnet.log.1")));
    Assert.Contains("Second entry", File.ReadAllText(Path.Combine(_directory, "computer-use-dotnet.log")));
  }

  public void Dispose()
  {
    if (Directory.Exists(_directory))
    {
      Directory.Delete(_directory, recursive: true);
    }
  }
}
