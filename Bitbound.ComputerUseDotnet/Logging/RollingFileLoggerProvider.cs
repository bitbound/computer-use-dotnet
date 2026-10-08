using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Bitbound.ComputerUseDotnet.Logging;

internal sealed class RollingFileLoggerProvider(string directory, long maxFileSizeBytes = 2 * 1024 * 1024, int retainedFileCount = 3)
  : ILoggerProvider
{
  private readonly object _fileLock = new();
  private readonly ConcurrentDictionary<string, FileLogger> _loggers = new(StringComparer.Ordinal);
  private readonly string _path = Path.Combine(directory, "computer-use-dotnet.log");

  public ILogger CreateLogger(string categoryName) =>
    _loggers.GetOrAdd(categoryName, category => new FileLogger(this, category));

  public void Dispose()
  {
  }

  private void EnsureRotation()
  {
    if (!File.Exists(_path) || new FileInfo(_path).Length < maxFileSizeBytes)
    {
      return;
    }

    var oldest = _path + $".{retainedFileCount}";
    if (File.Exists(oldest))
    {
      File.Delete(oldest);
    }

    for (var index = retainedFileCount - 1; index >= 1; index--)
    {
      var source = _path + $".{index}";
      if (File.Exists(source))
      {
        File.Move(source, _path + $".{index + 1}");
      }
    }

    File.Move(_path, _path + ".1");
  }

  private void Write(string category, LogLevel logLevel, EventId eventId, string message, Exception? exception)
  {
    lock (_fileLock)
    {
      Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
      EnsureRotation();

      using var writer = new StreamWriter(_path, append: true);
      writer.WriteLine($"{DateTimeOffset.Now:O} [{logLevel}] {category} ({eventId.Id}): {message}");

      if (exception is not null)
      {
        writer.WriteLine(exception);
      }
    }
  }

  private sealed class FileLogger(RollingFileLoggerProvider provider, string category) : ILogger
  {
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
      LogLevel logLevel,
      EventId eventId,
      TState state,
      Exception? exception,
      Func<TState, Exception?, string> formatter)
    {
      if (IsEnabled(logLevel))
      {
        provider.Write(category, logLevel, eventId, formatter(state, exception), exception);
      }
    }
  }
}
