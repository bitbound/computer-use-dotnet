using System.Collections;
using Bitbound.SystemAbstractions.FileSystem;
using Microsoft.Extensions.Logging;
using Tmds.DBus;

namespace Bitbound.ComputerUseDotnet.ComputerUse.Portal;

/// <summary>
/// Wayland backend: capture via the org.freedesktop.portal.Screenshot portal and input via
/// org.freedesktop.portal.RemoteDesktop (the only sanctioned Wayland input path).
/// The portal prompts the user once; the RemoteDesktop restore token is persisted so later
/// launches skip the prompt.
/// </summary>
internal sealed class WaylandBackend(
  ILogger<WaylandBackend> logger,
  IFileSystem fileSystem,
  RestoreTokenStore restoreTokens) : ComputerUseBackendBase
{
  private const uint DeviceTypeKeyboardAndPointer = 3;
  private const uint PersistModeRestoreToken = 2;

  private readonly IFileSystem _fileSystem = fileSystem;
  private readonly SemaphoreSlim _gate = new(1, 1);
  private readonly ILogger<WaylandBackend> _logger = logger;
  private readonly XdgPortalConnection _portal = new(logger);
  private readonly RestoreTokenStore _restoreTokens = restoreTokens;
  private readonly Lock _stateSync = new();

  private (int Width, int Height)? _lastScreenshotSize;
  private string? _sessionHandle;
  private List<PortalStream>? _streams;

  public override string BackendName => "Linux Wayland (XDG portal Screenshot + RemoteDesktop)";
  public override DesktopEnvironmentType EnvironmentType => DesktopEnvironmentType.Wayland;

  public override async Task<SKBitmap> CaptureVirtualScreenAsync(CancellationToken cancellationToken = default)
  {
    await _portal.ConnectAsync(cancellationToken);

    var handleToken = _portal.NextHandleToken();
    var proxy = _portal.CreateProxy<IXdgScreenshot>(XdgPortalConnection.PortalObjectPath);

    var (response, results) = await _portal.RequestAsync(
      handleToken,
      () => proxy.ScreenshotAsync(
        string.Empty,
        new Dictionary<string, object>
        {
          ["handle_token"] = handleToken,
          ["modal"] = true,
        }),
      cancellationToken: cancellationToken);

    if (response == 1)
    {
      throw new InvalidOperationException("The screenshot portal request was dismissed or cancelled by the user.");
    }

    if (response != 0)
    {
      throw new InvalidOperationException($"The screenshot portal request failed with response code {response}.");
    }

    if (!results.TryGetValue("uri", out var uriObject) || uriObject is not string uri || uri.Length == 0)
    {
      throw new InvalidOperationException("The screenshot portal returned no file URI.");
    }

    var localPath = uri.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
      ? new Uri(uri).LocalPath
      : uri;

    try
    {
      var bytes = await _fileSystem.ReadAllBytesAsync(localPath);
      var decoded = SKBitmap.Decode(bytes)
        ?? throw new InvalidOperationException($"Could not decode the screenshot PNG at {localPath}.");

      _lastScreenshotSize = (decoded.Width, decoded.Height);

      // When a RemoteDesktop session is active, normalize the image to logical stream space.
      List<PortalStream>? streams;

      lock (_stateSync)
      {
        streams = _sessionHandle is not null ? _streams : null;
      }

      if (streams is null || streams.Count == 0)
      {
        return decoded;
      }

      var layout = BuildLayoutFromStreams(streams);

      if (decoded.Width == layout.Width && decoded.Height == layout.Height)
      {
        return decoded;
      }

      var scaled = new SKBitmap(layout.Width, layout.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

      using (var canvas = new SKCanvas(scaled))
      using (decoded)
      using (var sourceImage = SKImage.FromBitmap(decoded))
      {
        canvas.Clear(SKColors.Black);
        canvas.DrawImage(
          sourceImage,
          new SKRect(0, 0, layout.Width, layout.Height),
          new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
      }

      _logger.LogInformation(
        "Scaled portal screenshot from {PixelWidth}x{PixelHeight} to logical {Width}x{Height}.",
        layout.Width,
        layout.Height,
        layout.Width,
        layout.Height);

      return scaled;
    }
    finally
    {
      try
      {
        if (_fileSystem.FileExists(localPath))
        {
          _fileSystem.DeleteFile(localPath);
        }
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "Could not delete the temporary portal screenshot at {Path}.", localPath);
      }
    }
  }

  public override Task<PermissionStatus> CheckPermissionsAsync(CancellationToken cancellationToken = default)
  {
    bool hasSession;

    lock (_stateSync)
    {
      hasSession = _sessionHandle is not null;
    }

    return Task.FromResult(new PermissionStatus(
      BackendName,
      PermissionState.Unknown,
      hasSession ? PermissionState.Granted : PermissionState.NotGranted,
      hasSession
        ? "A RemoteDesktop session is active, so input is granted. Screenshot permission state is unknown until a capture is attempted."
        : "Input requires a RemoteDesktop portal session (a permission prompt on first use). Screenshot permission state is unknown until a capture is attempted."));
  }

  /// <summary>The RemoteDesktop portal offers no cursor position query; always null on Wayland.</summary>
  public override Task<ScreenPoint?> GetCursorPositionAsync(CancellationToken cancellationToken = default) =>
    Task.FromResult<ScreenPoint?>(null);

  public override async Task<DisplayLayout> GetDisplayLayoutAsync(CancellationToken cancellationToken = default)
  {
    List<PortalStream>? streams;

    lock (_stateSync)
    {
      streams = _sessionHandle is not null ? _streams : null;
    }

    if (streams is { Count: > 0 })
    {
      return BuildLayoutFromStreams(streams);
    }

    if (_lastScreenshotSize is { } cached)
    {
      return SingleDisplayLayout(cached.Width, cached.Height);
    }

    // No active session and nothing cached: the screenshot is the only size source available
    // without triggering the (input) permission prompt.
    var bitmap = await CaptureVirtualScreenAsync(cancellationToken);

    using (bitmap)
    {
      return SingleDisplayLayout(bitmap.Width, bitmap.Height);
    }
  }

  public override async Task MovePointerAsync(ScreenPoint point, CancellationToken cancellationToken = default)
  {
    var session = await EnsureSessionAsync(cancellationToken);
    var streams = GetActiveStreams();
    var layout = BuildLayoutFromStreams(streams);
    var native = layout.ToNative(layout.Clamp(point));

    var stream = PickStream(streams, native);
    var ratioX = Math.Clamp((double)(native.X - stream.X) / stream.Width, 0d, 1d);
    var ratioY = Math.Clamp((double)(native.Y - stream.Y) / stream.Height, 0d, 1d);

    var proxy = _portal.CreateProxy<IXdgRemoteDesktop>(XdgPortalConnection.PortalObjectPath);
    await proxy.NotifyPointerMotionAbsoluteAsync(session, EmptyOptions(), stream.Id, ratioX, ratioY);
  }

  public override async Task PressChordAsync(KeyChord chord, CancellationToken cancellationToken = default)
  {
    var session = await EnsureSessionAsync(cancellationToken);
    var proxy = _portal.CreateProxy<IXdgRemoteDesktop>(XdgPortalConnection.PortalObjectPath);

    var pressed = new List<uint>();

    try
    {
      foreach (var modifier in chord.Modifiers)
      {
        var keycode = (uint)LinuxKeycodes.GetModifierKeycode(modifier);
        pressed.Add(keycode);
        await proxy.NotifyKeyboardKeycodeAsync(session, EmptyOptions(), keycode, 1u);
      }

      uint targetKeycode;
      var shifted = false;

      if (chord.Target.Character is { } character)
      {
        if (!LinuxKeycodes.TryGetCharacter(character, out var mapped, out shifted))
        {
          throw new InvalidOperationException($"Character '{character}' has no US-layout evdev key mapping.");
        }

        targetKeycode = (uint)mapped;
      }
      else if (chord.Target.Name is { } name)
      {
        if (!LinuxKeycodes.TryGetKeycode(name, out var named))
        {
          throw new InvalidOperationException($"Key '{name}' has no evdev key mapping.");
        }

        targetKeycode = (uint)named;
      }
      else
      {
        throw new InvalidOperationException("Key chord has no target key.");
      }

      if (shifted)
      {
        var shiftKeycode = (uint)LinuxKeycodes.KEY_LEFTSHIFT;
        pressed.Add(shiftKeycode);
        await proxy.NotifyKeyboardKeycodeAsync(session, EmptyOptions(), shiftKeycode, 1u);
      }

      await proxy.NotifyKeyboardKeycodeAsync(session, EmptyOptions(), targetKeycode, 1u);
      await proxy.NotifyKeyboardKeycodeAsync(session, EmptyOptions(), targetKeycode, 0u);
    }
    finally
    {
      for (var i = pressed.Count - 1; i >= 0; i--)
      {
        await proxy.NotifyKeyboardKeycodeAsync(session, EmptyOptions(), pressed[i], 0u);
      }
    }
  }

  public override async Task<PermissionStatus> RequestPermissionsAsync(CancellationToken cancellationToken = default)
  {
    PermissionState input;

    try
    {
      await EnsureSessionAsync(cancellationToken);
      input = PermissionState.Granted;
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      _logger.LogWarning(ex, "RemoteDesktop session establishment failed during request_permissions.");
      input = PermissionState.NotGranted;
    }

    PermissionState capture;

    try
    {
      var bitmap = await CaptureVirtualScreenAsync(cancellationToken);
      bitmap.Dispose();
      capture = PermissionState.Granted;
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      _logger.LogWarning(ex, "Portal screenshot failed during request_permissions.");
      capture = PermissionState.NotGranted;
    }

    return new PermissionStatus(
      BackendName,
      capture,
      input,
      "Portal grants persist per application. The RemoteDesktop restore token was saved on success; the Screenshot portal has no restore token and may re-prompt depending on the portal implementation.");
  }

  public override async Task ScrollAsync(ScreenPoint point, int verticalClicks, int horizontalClicks, CancellationToken cancellationToken = default)
  {
    await MovePointerAsync(point, cancellationToken);

    if (verticalClicks == 0 && horizontalClicks == 0)
    {
      return;
    }

    var session = await EnsureSessionAsync(cancellationToken);
    var proxy = _portal.CreateProxy<IXdgRemoteDesktop>(XdgPortalConnection.PortalObjectPath);

    // Portal axis 0 = vertical (positive = up), axis 1 = horizontal (positive = right).
    if (verticalClicks != 0)
    {
      await proxy.NotifyPointerAxisDiscreteAsync(session, EmptyOptions(), 0u, verticalClicks);
    }

    if (horizontalClicks != 0)
    {
      await proxy.NotifyPointerAxisDiscreteAsync(session, EmptyOptions(), 1u, horizontalClicks);
    }
  }

  public override async Task SetPointerButtonAsync(MouseButton button, bool pressed, CancellationToken cancellationToken = default)
  {
    var session = await EnsureSessionAsync(cancellationToken);
    var proxy = _portal.CreateProxy<IXdgRemoteDesktop>(XdgPortalConnection.PortalObjectPath);

    await proxy.NotifyPointerButtonAsync(
      session,
      EmptyOptions(),
      (uint)LinuxKeycodes.GetButtonCode(button),
      pressed ? 1u : 0u);
  }

  public override async Task TypeTextAsync(string text, CancellationToken cancellationToken = default)
  {
    var session = await EnsureSessionAsync(cancellationToken);
    var proxy = _portal.CreateProxy<IXdgRemoteDesktop>(XdgPortalConnection.PortalObjectPath);
    var skipped = 0;

    foreach (var character in text)
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (character is '\r')
      {
        continue;
      }

      if (!LinuxKeycodes.TryGetCharacter(character, out var keycode, out var requiresShift))
      {
        skipped++;
        continue;
      }

      var shiftKeycode = (uint)LinuxKeycodes.KEY_LEFTSHIFT;

      if (requiresShift)
      {
        await proxy.NotifyKeyboardKeycodeAsync(session, EmptyOptions(), shiftKeycode, 1u);
      }

      await proxy.NotifyKeyboardKeycodeAsync(session, EmptyOptions(), (uint)keycode, 1u);
      await proxy.NotifyKeyboardKeycodeAsync(session, EmptyOptions(), (uint)keycode, 0u);

      if (requiresShift)
      {
        await proxy.NotifyKeyboardKeycodeAsync(session, EmptyOptions(), shiftKeycode, 0u);
      }
    }

    if (skipped > 0)
    {
      _logger.LogWarning("Skipped {Count} characters with no US-layout evdev mapping while typing.", skipped);
    }

    if (skipped == text.Length && text.Length > 0)
    {
      throw new InvalidOperationException("No characters could be typed; none had a US-layout evdev key mapping.");
    }
  }

  protected override void DisposeCore()
  {
    _portal.Dispose();
    _gate.Dispose();
  }

  private static DisplayLayout BuildLayoutFromStreams(List<PortalStream> streams)
  {
    var displays = new List<DisplayInfo>();

    for (var i = 0; i < streams.Count; i++)
    {
      var stream = streams[i];

      displays.Add(new DisplayInfo
      {
        Index = i,
        Name = $"Stream {stream.Id}",
        X = stream.X,
        Y = stream.Y,
        Width = stream.Width,
        Height = stream.Height,
        IsPrimary = i == 0,
        Scale = 1,
      });
    }

    return new DisplayLayout(displays);
  }

  private static Dictionary<string, object> EmptyOptions() => new();

  private static object? GetValue(IDictionary<string, object>? dict, string key)  {
    if (dict is null)
    {
      return null;
    }

    return dict.TryGetValue(key, out var value) ? value : null;
  }

  private static List<PortalStream> ParseStreams(IDictionary<string, object> results)
  {
    var streams = new List<PortalStream>();

    if (!results.TryGetValue("streams", out var streamsObject) || streamsObject is not IEnumerable enumerable)
    {
      return streams;
    }

    foreach (var entry in enumerable)
    {
      uint streamId;
      IDictionary<string, object>? props;

      if (entry is ValueTuple<uint, IDictionary<string, object>> tuple)
      {
        streamId = tuple.Item1;
        props = tuple.Item2;
      }
      else if (entry is IDictionary dict && dict.Contains(0) && dict.Contains(1))
      {
        streamId = Convert.ToUInt32(dict[0]);
        props = dict[1] as IDictionary<string, object>;
      }
      else
      {
        var fields = entry?.GetType()?.GetFields() ?? [];

        if (fields.Length < 2)
        {
          continue;
        }

        streamId = Convert.ToUInt32(fields[0].GetValue(entry));
        props = fields[1].GetValue(entry) as IDictionary<string, object>;
      }

      var (x, y) = ReadIntPair(props, "position");
      var (width, height) = ReadIntPair(props, "size");

      if (width <= 0 || height <= 0)
      {
        continue;
      }

      streams.Add(new PortalStream(streamId, x, y, width, height));
    }

    return streams;
  }

  private static PortalStream PickStream(List<PortalStream> streams, ScreenPoint native)
  {
    var containing = streams.FirstOrDefault(s =>
      native.X >= s.X && native.X < s.X + s.Width &&
      native.Y >= s.Y && native.Y < s.Y + s.Height);

    if (containing.Width > 0 && containing.Height > 0)
    {
      return containing;
    }

    // Out of bounds: use the nearest stream so clamped edge coordinates still land somewhere sane.
    var best = streams[0];
    var bestDistance = long.MaxValue;

    foreach (var stream in streams)
    {
      var nearestX = Math.Clamp(native.X, stream.X, stream.X + stream.Width - 1);
      var nearestY = Math.Clamp(native.Y, stream.Y, stream.Y + stream.Height - 1);
      var distance = ((long)(native.X - nearestX) * (native.X - nearestX)) +
                     ((long)(native.Y - nearestY) * (native.Y - nearestY));

      if (distance < bestDistance)
      {
        bestDistance = distance;
        best = stream;
      }
    }

    return best;
  }

  private static (int X, int Y) ReadIntPair(IDictionary<string, object>? props, string key)
  {
    if (GetValue(props, key) is not IEnumerable enumerable)
    {
      return (0, 0);
    }

    var values = new List<int>();

    foreach (var item in enumerable)
    {
      if (item is ValueTuple<int, int> pair)
      {
        return (pair.Item1, pair.Item2);
      }

      try
      {
        values.Add(Convert.ToInt32(item));
      }
      catch (Exception)
      {
        return (0, 0);
      }
    }

    return values.Count >= 2 ? (values[0], values[1]) : (0, 0);
  }

  private static DisplayLayout SingleDisplayLayout(int width, int height) =>
    new(
    [
      new DisplayInfo
      {
        Index = 0,
        Name = "Wayland screen",
        X = 0,
        Y = 0,
        Width = width,
        Height = height,
        IsPrimary = true,
        Scale = 1,
      },
    ]);

  /// <summary>Establishes (or returns the existing) RemoteDesktop session, prompting once.</summary>
  private async Task<ObjectPath> EnsureSessionAsync(CancellationToken cancellationToken)
  {
    string? handle;

    lock (_stateSync)
    {
      handle = _sessionHandle;
    }

    if (handle is not null)
    {
      return new ObjectPath(handle);
    }

    await _gate.WaitAsync(cancellationToken);

    try
    {
      lock (_stateSync)
      {
        if (_sessionHandle is not null)
        {
          return new ObjectPath(_sessionHandle);
        }
      }

      await _portal.ConnectAsync(cancellationToken);

      var savedToken = _restoreTokens.TryLoad();

      try
      {
        await EstablishSessionCoreAsync(savedToken, cancellationToken);
      }
      catch (RestoreTokenRejectedException) when (savedToken is not null)
      {
        // Only a rejected restore token warrants a tokenless retry; other failures
        // (dismissed prompt, portal error) must not discard a still-valid grant or re-prompt.
        _logger.LogInformation("Restore token was rejected; retrying the portal session without it.");
        _restoreTokens.Clear();
        await EstablishSessionCoreAsync(null, cancellationToken);
      }

      return new ObjectPath(_sessionHandle!);
    }
    finally
    {
      _gate.Release();
    }
  }

  private async Task EstablishSessionCoreAsync(string? restoreToken, CancellationToken cancellationToken)
  {
    var proxy = _portal.CreateProxy<IXdgRemoteDesktop>(XdgPortalConnection.PortalObjectPath);

    // 1) CreateSession
    var createToken = _portal.NextHandleToken();
    var sessionToken = $"cuse{createToken}";

    var (createResponse, createResults) = await _portal.RequestAsync(
      createToken,
      () => proxy.CreateSessionAsync(new Dictionary<string, object>
      {
        ["handle_token"] = createToken,
        ["session_handle_token"] = sessionToken,
      }),
      cancellationToken: cancellationToken);

    if (createResponse != 0)
    {
      throw new InvalidOperationException($"RemoteDesktop CreateSession failed with response code {createResponse}.");
    }

    if (GetValue(createResults, "session_handle") is not string sessionHandle)
    {
      throw new InvalidOperationException("RemoteDesktop CreateSession returned no session handle.");
    }

    // 2) SelectDevices (keyboard + pointer, persist via restore token)
    var selectToken = _portal.NextHandleToken();
    var selectOptions = new Dictionary<string, object>
    {
      ["handle_token"] = selectToken,
      ["types"] = DeviceTypeKeyboardAndPointer,
      ["persist_mode"] = PersistModeRestoreToken,
    };

    if (restoreToken is not null)
    {
      selectOptions["restore_token"] = restoreToken;
    }

    var (selectResponse, _) = await _portal.RequestAsync(
      selectToken,
      () => proxy.SelectDevicesAsync(new ObjectPath(sessionHandle), selectOptions),
      cancellationToken: cancellationToken);

    if (selectResponse != 0)
    {
      throw restoreToken is not null
        ? new RestoreTokenRejectedException($"RemoteDesktop SelectDevices rejected the restore token with response code {selectResponse}.")
        : new InvalidOperationException($"RemoteDesktop SelectDevices failed with response code {selectResponse}.");
    }

    // 3) Start (may re-prompt unless the token persists the grant)
    var startToken = _portal.NextHandleToken();

    var (startResponse, startResults) = await _portal.RequestAsync(
      startToken,
      () => proxy.StartAsync(new ObjectPath(sessionHandle), string.Empty, new Dictionary<string, object>
      {
        ["handle_token"] = startToken,
      }),
      cancellationToken: cancellationToken);

    if (startResponse == 1)
    {
      throw new InvalidOperationException("The RemoteDesktop permission prompt was dismissed or cancelled by the user.");
    }

    if (startResponse != 0)
    {
      throw new InvalidOperationException($"RemoteDesktop Start failed with response code {startResponse}.");
    }

    if (GetValue(startResults, "restore_token") is string newToken && newToken.Length > 0)
    {
      _restoreTokens.Save(newToken);
    }

    var streams = ParseStreams(startResults);

    if (streams.Count == 0)
    {
      throw new InvalidOperationException("RemoteDesktop Start succeeded but reported no streams; cannot map coordinates.");
    }

    // Publish streams before the handle under the state lock: readers take the
    // handle as the "session ready" flag and may run on weakly-ordered CPUs.
    lock (_stateSync)
    {
      _streams = streams;
      _sessionHandle = sessionHandle;
    }

    _logger.LogInformation("RemoteDesktop session established with {StreamCount} stream(s).", streams.Count);
  }

  private List<PortalStream> GetActiveStreams()
  {
    lock (_stateSync)
    {
      if (_sessionHandle is null || _streams is not { Count: > 0 })
      {
        throw new InvalidOperationException("No RemoteDesktop streams are available.");
      }

      return _streams;
    }
  }

  private readonly record struct PortalStream(uint Id, int X, int Y, int Width, int Height);

  /// <summary>Thrown when SelectDevices rejects a persisted restore token (worth a tokenless retry).</summary>
  private sealed class RestoreTokenRejectedException(string message) : InvalidOperationException(message)
  {
  }
}
