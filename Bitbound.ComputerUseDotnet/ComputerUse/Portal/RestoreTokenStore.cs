using Bitbound.SystemAbstractions.FileSystem;
using Microsoft.Extensions.Logging;

namespace Bitbound.ComputerUseDotnet.ComputerUse.Portal;

/// <summary>
/// Persists the RemoteDesktop portal restore token so relaunches do not re-prompt.
/// Stored under the user config directory with owner-only file permissions.
/// </summary>
internal sealed class RestoreTokenStore(IFileSystem fileSystem, ILogger<RestoreTokenStore> logger)
{
  private const string ConfigDirectoryName = "computer-use-dotnet";
  private const string TokenFileName = "wayland-remotedesktop-restore-token";

  private readonly ILogger<RestoreTokenStore> _logger = logger;
  private readonly IFileSystem _fileSystem = fileSystem;
  private readonly Lock _sync = new();

  private string? _cachedToken;

  public string FilePath
  {
    get
    {
      var configRoot = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");

      if (string.IsNullOrWhiteSpace(configRoot))
      {
        configRoot = Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
          ".config");
      }

      return Path.Combine(configRoot, ConfigDirectoryName, TokenFileName);
    }
  }

  /// <summary>Returns the saved restore token, or null when none exists.</summary>
  public string? TryLoad()
  {
    lock (_sync)
    {
      if (_cachedToken is not null)
      {
        return _cachedToken;
      }

      var path = FilePath;

      if (!_fileSystem.FileExists(path))
      {
        return null;
      }

      try
      {
        var token = _fileSystem.ReadAllText(path).Trim();

        if (token.Length == 0)
        {
          return null;
        }

        _cachedToken = token;
        _logger.LogDebug("Loaded RemoteDesktop restore token from {Path}.", path);
        return token;
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "Failed to read the restore token at {Path}.", path);
        return null;
      }
    }
  }

  /// <summary>Saves a restore token (rotate on every successful Start).</summary>
  public void Save(string token)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(token);

    lock (_sync)
    {
      var path = FilePath;
      var directory = Path.GetDirectoryName(path)!;

      if (!_fileSystem.DirectoryExists(directory))
      {
        _fileSystem.CreateDirectory(directory);
      }

      _fileSystem.WriteAllText(path, token);

      if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
      {
        try
        {
          _fileSystem.SetUnixFileMode(
            path,
            UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch (Exception ex)
        {
          _logger.LogWarning(ex, "Saved the restore token but could not restrict permissions on {Path}.", path);
        }
      }

      _cachedToken = token.Trim();
      _logger.LogInformation("Saved RemoteDesktop restore token to {Path}.", path);
    }
  }

  /// <summary>Discards a stale or rejected token.</summary>
  public void Clear()
  {
    lock (_sync)
    {
      _cachedToken = null;

      var path = FilePath;

      if (_fileSystem.FileExists(path))
      {
        try
        {
          _fileSystem.DeleteFile(path);
        }
        catch (Exception ex)
        {
          _logger.LogWarning(ex, "Failed to delete the restore token file at {Path}.", path);
        }
      }
    }
  }
}
