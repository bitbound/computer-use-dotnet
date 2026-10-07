using Bitbound.ComputerUseDotnet.ComputerUse.Portal;
using Bitbound.SystemAbstractions.TestUtilities.FileSystem;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bitbound.ComputerUseDotnet.Tests;

/// <summary>Tests for <see cref="RestoreTokenStore"/> persistence and rotation.</summary>
public sealed class RestoreTokenStoreTests : IDisposable
{
  private readonly string? _originalConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");

  public RestoreTokenStoreTests()
  {
    Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", "/config");
  }

  public void Dispose() =>
    Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _originalConfigHome);

  [Fact]
  public void TryLoad_WithoutSavedToken_ReturnsNull()
  {
    var store = CreateStore(out _);

    Assert.Null(store.TryLoad());
  }

  [Fact]
  public void SaveThenLoad_RoundTripsThroughFileSystem()
  {
    var fileSystem = new FakeFileSystem('/');
    var store = new RestoreTokenStore(fileSystem, NullLogger<RestoreTokenStore>.Instance);

    store.Save("token-abc");

    Assert.True(fileSystem.FileExists("/config/computer-use-dotnet/wayland-remotedesktop-restore-token"));

    // A fresh store instance must see the persisted token (simulates a server relaunch).
    var secondStore = new RestoreTokenStore(fileSystem, NullLogger<RestoreTokenStore>.Instance);
    Assert.Equal("token-abc", secondStore.TryLoad());
  }

  [Fact]
  public void Clear_RemovesPersistedToken()
  {
    var fileSystem = new FakeFileSystem('/');
    var store = new RestoreTokenStore(fileSystem, NullLogger<RestoreTokenStore>.Instance);

    store.Save("token-abc");
    store.Clear();

    Assert.False(fileSystem.FileExists("/config/computer-use-dotnet/wayland-remotedesktop-restore-token"));

    var secondStore = new RestoreTokenStore(fileSystem, NullLogger<RestoreTokenStore>.Instance);
    Assert.Null(secondStore.TryLoad());
  }

  private static RestoreTokenStore CreateStore(out FakeFileSystem fileSystem)
  {
    fileSystem = new FakeFileSystem('/');
    return new RestoreTokenStore(fileSystem, NullLogger<RestoreTokenStore>.Instance);
  }
}
