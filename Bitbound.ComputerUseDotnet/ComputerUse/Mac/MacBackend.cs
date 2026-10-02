using Microsoft.Extensions.Logging;

namespace Bitbound.ComputerUseDotnet.ComputerUse.Mac;

/// <summary>
/// macOS capture via CoreGraphics CGDisplayCreateImage and input via CGEvent posting.
/// Requires Screen Recording (capture) and Accessibility (input) permissions; use
/// request_permissions to trigger the prompts.
/// </summary>
internal sealed class MacBackend(ILogger<MacBackend> logger) : ComputerUseBackendBase
{
  private const uint MaxDisplays = 64;

  private readonly ILogger<MacBackend> _logger = logger;
  private readonly nint _eventSource = MacBindings.CGEventSourceCreate((uint)MacBindings.kCGEventSourceStateHIDSystemState);

  public override string BackendName => "macOS (CoreGraphics capture + CGEvent input)";

  public override DesktopEnvironmentType EnvironmentType => DesktopEnvironmentType.MacOS;

  public override unsafe Task<SKBitmap> CaptureVirtualScreenAsync(CancellationToken cancellationToken = default)
  {
    var layout = BuildLayout();
    var composite = new SKBitmap(layout.Width, layout.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

    using (var canvas = new SKCanvas(composite))
    {
      canvas.Clear(SKColors.Black);

      foreach (var display in layout.Displays)
      {
        var nameParts = display.Name.Split(' ');

        if (nameParts.Length < 2 || !uint.TryParse(nameParts[1], System.Globalization.CultureInfo.InvariantCulture, out var displayId))
        {
          _logger.LogWarning("Display name '{Name}' does not carry a CGDirectDisplayID; skipping it.", display.Name);
          continue;
        }

        var imageRef = MacBindings.CGDisplayCreateImage(displayId);

        if (imageRef == nint.Zero)
        {
          _logger.LogWarning("CGDisplayCreateImage returned nil for display {DisplayId}.", displayId);
          continue;
        }

        try
        {
          var bitmap = CgImageToSkBitmap(imageRef);

          if (bitmap is null)
          {
            _logger.LogWarning("Failed to convert CGImage for display {DisplayId}.", displayId);
            continue;
          }

          using (bitmap)
          using (var sourceImage = SKImage.FromBitmap(bitmap))
          {
            // Draw physical-resolution image scaled down into logical (point) space.
            var destination = new SKRect(
              display.X - layout.OriginX,
              display.Y - layout.OriginY,
              display.X - layout.OriginX + display.Width,
              display.Y - layout.OriginY + display.Height);

            canvas.DrawImage(
              sourceImage,
              destination,
              new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
          }
        }
        finally
        {
          MacBindings.CFRelease(imageRef);
        }
      }
    }

    return Task.FromResult(composite);
  }

  public override Task<DisplayLayout> GetDisplayLayoutAsync(CancellationToken cancellationToken = default) =>
    Task.FromResult(BuildLayout());

  public override Task MovePointerAsync(ScreenPoint point, CancellationToken cancellationToken = default)
  {
    var native = PointToNative(point);
    MacBindings.CGWarpMouseCursorPosition(new MacBindings.CGPoint(native.X, native.Y));
    PostMouse(MacBindings.kCGEventMouseMoved, MacBindings.kCGMouseButtonLeft, native);
    return Task.CompletedTask;
  }

  public override Task SetPointerButtonAsync(MouseButton button, bool pressed, CancellationToken cancellationToken = default)
  {
    var location = CurrentNativeCursor();
    var (type, cgButton) = button switch
    {
      MouseButton.Left => (pressed ? MacBindings.kCGEventLeftMouseDown : MacBindings.kCGEventLeftMouseUp, MacBindings.kCGMouseButtonLeft),
      MouseButton.Right => (pressed ? MacBindings.kCGEventRightMouseDown : MacBindings.kCGEventRightMouseUp, MacBindings.kCGMouseButtonRight),
      MouseButton.Middle => (pressed ? MacBindings.kCGEventOtherMouseDown : MacBindings.kCGEventOtherMouseUp, 2u),
      MouseButton.Extra => (pressed ? MacBindings.kCGEventOtherMouseDown : MacBindings.kCGEventOtherMouseUp, 3u),
      MouseButton.Side => (pressed ? MacBindings.kCGEventOtherMouseDown : MacBindings.kCGEventOtherMouseUp, 4u),
      _ => throw new ArgumentOutOfRangeException(nameof(button), button, null),
    };

    var eventRef = MacBindings.CGEventCreateMouseEvent(_eventSource, type, location, cgButton);

    if (eventRef == nint.Zero)
    {
      throw new InvalidOperationException("CGEventCreateMouseEvent returned nil.");
    }

    try
    {
      MacBindings.CGEventPost(MacBindings.kCGHIDEventTap, eventRef);
    }
    finally
    {
      MacBindings.CFRelease(eventRef);
    }

    return Task.CompletedTask;
  }

  public override async Task ScrollAsync(ScreenPoint point, int verticalClicks, int horizontalClicks, CancellationToken cancellationToken = default)
  {
    await MovePointerAsync(point, cancellationToken);

    if (verticalClicks == 0 && horizontalClicks == 0)
    {
      return;
    }

    var eventRef = MacBindings.CGEventCreateScrollWheelEvent(
      _eventSource,
      MacBindings.kCGScrollEventUnitLine,
      2,
      verticalClicks,
      horizontalClicks);

    if (eventRef == nint.Zero)
    {
      throw new InvalidOperationException("CGEventCreateScrollWheelEvent returned nil.");
    }

    try
    {
      MacBindings.CGEventPost(MacBindings.kCGHIDEventTap, eventRef);
    }
    finally
    {
      MacBindings.CFRelease(eventRef);
    }
  }

  public override async Task TypeTextAsync(string text, CancellationToken cancellationToken = default)
  {
    foreach (var character in text)
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (character is '\r')
      {
        continue;
      }

      if (character is '\n')
      {
        PostKeyboardKey(MacVirtualKeys.kVK_Return, flags: 0, keyDown: true);
        PostKeyboardKey(MacVirtualKeys.kVK_Return, flags: 0, keyDown: false);
        continue;
      }

      // Unicode injection handles any character regardless of the active keyboard layout.
      PostUnicodeChar(character);
    }

    await Task.CompletedTask;
  }

  public override Task PressChordAsync(KeyChord chord, CancellationToken cancellationToken = default)
  {
    var flags = 0UL;
    var modifierKeys = new List<ushort>();

    foreach (var modifier in chord.Modifiers)
    {
      modifierKeys.Add(MacVirtualKeys.GetModifierVirtualKey(modifier));
      flags |= MacVirtualKeys.GetModifierFlag(modifier);
    }

    var shifted = false;
    ushort targetKey = 0;
    var unicodeFallback = false;
    char unicodeChar = '\0';

    if (chord.Target.Character is { } character)
    {
      if (MacVirtualKeys.TryGetCharacter(character, out targetKey, out shifted))
      {
        if (shifted)
        {
          flags |= MacVirtualKeys.GetModifierFlag(ModifierKey.Shift);
        }
      }
      else
      {
        unicodeFallback = true;
        unicodeChar = character;
      }
    }
    else if (chord.Target.Name is { } name)
    {
      if (!MacVirtualKeys.TryGetVirtualKey(name, out targetKey))
      {
        throw new InvalidOperationException($"Key '{name}' has no macOS virtual-key mapping.");
      }
    }

    foreach (var modifierKey in modifierKeys)
    {
      PostKeyboardKey(modifierKey, flags, keyDown: true);
    }

    if (unicodeFallback)
    {
      PostUnicodeChar(unicodeChar, flags);
    }
    else
    {
      PostKeyboardKey(targetKey, flags, keyDown: true);
      PostKeyboardKey(targetKey, flags, keyDown: false);
    }

    for (var i = modifierKeys.Count - 1; i >= 0; i--)
    {
      PostKeyboardKey(modifierKeys[i], flags, keyDown: false);
    }

    return Task.CompletedTask;
  }

  public override async Task<ScreenPoint?> GetCursorPositionAsync(CancellationToken cancellationToken = default)
  {
    var locationEvent = MacBindings.CGEventCreate(nint.Zero);

    if (locationEvent == nint.Zero)
    {
      return null;
    }

    try
    {
      var location = MacBindings.CGEventGetLocation(locationEvent);
      var layout = await GetDisplayLayoutAsync(cancellationToken);

      return new ScreenPoint(
        (int)Math.Round(location.X) - layout.OriginX,
        (int)Math.Round(location.Y) - layout.OriginY);
    }
    finally
    {
      MacBindings.CFRelease(locationEvent);
    }
  }

  public override Task<PermissionStatus> CheckPermissionsAsync(CancellationToken cancellationToken = default)
  {
    var input = SafeCall(MacBindings.AXIsProcessTrusted) ? PermissionState.Granted : PermissionState.NotGranted;
    var capture = PreflightScreenCapture();

    return Task.FromResult(new PermissionStatus(
      BackendName,
      capture,
      input,
      "Screen Recording gates capture; Accessibility (AXIsProcessTrusted) gates input. Call request_permissions to show the prompts, then relaunch this MCP server after granting."));
  }

  public override Task<PermissionStatus> RequestPermissionsAsync(CancellationToken cancellationToken = default)
  {
    var captureRequested = SafeCall(MacBindings.CGRequestScreenCaptureAccess);

    var options = MacBindings.CreateAccessibilityPromptOptions();
    var trusted = false;

    if (options != nint.Zero)
    {
      try
      {
        trusted = SafeCall(() => MacBindings.AXIsProcessTrustedWithOptions(options));
      }
      finally
      {
        MacBindings.CFRelease(options);
      }
    }

    _logger.LogInformation(
      "Requested macOS permissions: screen capture granted={Capture}, accessibility granted={Accessibility}.",
      captureRequested,
      trusted);

    return Task.FromResult(new PermissionStatus(
      BackendName,
      captureRequested ? PermissionState.Granted : PermissionState.NotGranted,
      trusted ? PermissionState.Granted : PermissionState.NotGranted,
      "Prompts were shown if the system allowed them. macOS usually requires relaunching this MCP server for grants to take effect; a terminal/CLI host may be added to TCC in place of this executable."));
  }

  protected override void DisposeCore()
  {
    if (_eventSource != nint.Zero)
    {
      MacBindings.CFRelease(_eventSource);
    }
  }

  private static bool SafeCall(Func<bool> call)
  {
    try
    {
      return call();
    }
    catch (EntryPointNotFoundException)
    {
      return false;
    }
    catch (DllNotFoundException)
    {
      return false;
    }
  }

  private static PermissionState PreflightScreenCapture()
  {
    try
    {
      return MacBindings.CGPreflightScreenCaptureAccess() ? PermissionState.Granted : PermissionState.NotGranted;
    }
    catch (EntryPointNotFoundException)
    {
      return PermissionState.Unknown;
    }
    catch (DllNotFoundException)
    {
      return PermissionState.Unknown;
    }
  }

  private static unsafe SKBitmap? CgImageToSkBitmap(nint imageRef)
  {
    var width = (int)MacBindings.CGImageGetWidth(imageRef);
    var height = (int)MacBindings.CGImageGetHeight(imageRef);
    var bitsPerPixel = (int)MacBindings.CGImageGetBitsPerPixel(imageRef);
    var bytesPerRow = (int)MacBindings.CGImageGetBytesPerRow(imageRef);

    if (width <= 0 || height <= 0 || bitsPerPixel != 32)
    {
      return null;
    }

    var provider = MacBindings.CGImageGetDataProvider(imageRef);

    if (provider == nint.Zero)
    {
      return null;
    }

    var data = MacBindings.CGDataProviderCopyData(provider);

    if (data == nint.Zero)
    {
      return null;
    }

    try
    {
      var dataPointer = MacBindings.CFDataGetBytePtr(data);
      var dataLength = (int)MacBindings.CFDataGetLength(data);

      if (dataPointer == nint.Zero || dataLength == 0)
      {
        return null;
      }

      var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
      var destination = bitmap.GetPixels();

      if (destination == nint.Zero)
      {
        bitmap.Dispose();
        return null;
      }

      var source = (byte*)dataPointer;
      var target = (byte*)destination;

      // CGImage from CGDisplayCreateImage is little-endian BGRA; copy row by row.
      for (var y = 0; y < height; y++)
      {
        var sourceRow = source + ((long)y * bytesPerRow);
        var destinationRow = target + ((long)y * bitmap.RowBytes);
        Buffer.MemoryCopy(sourceRow, destinationRow, bitmap.RowBytes, (long)width * 4);
      }

      return bitmap;
    }
    finally
    {
      MacBindings.CFRelease(data);
    }
  }

  private MacBindings.CGPoint PointToNative(ScreenPoint point)
  {
    var layout = GetDisplayLayoutAsync().GetAwaiter().GetResult();
    var native = layout.ToNative(point);
    return new MacBindings.CGPoint(native.X, native.Y);
  }

  private MacBindings.CGPoint CurrentNativeCursor()
  {
    var locationEvent = MacBindings.CGEventCreate(nint.Zero);

    if (locationEvent == nint.Zero)
    {
      return new MacBindings.CGPoint(0, 0);
    }

    try
    {
      return MacBindings.CGEventGetLocation(locationEvent);
    }
    finally
    {
      MacBindings.CFRelease(locationEvent);
    }
  }

  private void PostMouse(uint type, uint button, MacBindings.CGPoint location)
  {
    var eventRef = MacBindings.CGEventCreateMouseEvent(_eventSource, type, location, button);

    if (eventRef == nint.Zero)
    {
      return;
    }

    try
    {
      MacBindings.CGEventPost(MacBindings.kCGHIDEventTap, eventRef);
    }
    finally
    {
      MacBindings.CFRelease(eventRef);
    }
  }

  private void PostKeyboardKey(ushort virtualKey, ulong flags, bool keyDown)
  {
    var eventRef = MacBindings.CGEventCreateKeyboardEvent(_eventSource, virtualKey, keyDown);

    if (eventRef == nint.Zero)
    {
      throw new InvalidOperationException("CGEventCreateKeyboardEvent returned nil; is Accessibility permission granted?");
    }

    try
    {
      if (flags != 0)
      {
        MacBindings.CGEventSetFlags(eventRef, flags);
      }

      MacBindings.CGEventPost(MacBindings.kCGHIDEventTap, eventRef);
    }
    finally
    {
      MacBindings.CFRelease(eventRef);
    }
  }

  private void PostUnicodeChar(char character, ulong flags = 0)
  {
    var eventRef = MacBindings.CGEventCreateKeyboardEvent(_eventSource, 0, keyDown: true);

    if (eventRef == nint.Zero)
    {
      throw new InvalidOperationException("CGEventCreateKeyboardEvent returned nil; is Accessibility permission granted?");
    }

    var upRef = MacBindings.CGEventCreateKeyboardEvent(_eventSource, 0, keyDown: false);

    try
    {
      var characters = character.ToString();
      MacBindings.CGEventKeyboardSetUnicodeString(eventRef, 1, characters);

      if (flags != 0)
      {
        MacBindings.CGEventSetFlags(eventRef, flags);
      }

      MacBindings.CGEventPost(MacBindings.kCGHIDEventTap, eventRef);

      if (upRef != nint.Zero)
      {
        MacBindings.CGEventKeyboardSetUnicodeString(upRef, 1, characters);

        if (flags != 0)
        {
          MacBindings.CGEventSetFlags(upRef, flags);
        }

        MacBindings.CGEventPost(MacBindings.kCGHIDEventTap, upRef);
      }
    }
    finally
    {
      MacBindings.CFRelease(eventRef);

      if (upRef != nint.Zero)
      {
        MacBindings.CFRelease(upRef);
      }
    }
  }

  private DisplayLayout BuildLayout()
  {
    var ids = new uint[MaxDisplays];
    var result = MacBindings.CGGetOnlineDisplayList((uint)ids.Length, ids, out var count);

    if (result != 0)
    {
      // Retry once with a bigger buffer in case displays were hot-plugged mid-call.
      ids = new uint[Math.Max(MaxDisplays, ids.Length * 2)];
      result = MacBindings.CGGetOnlineDisplayList((uint)ids.Length, ids, out count);

      if (result != 0)
      {
        throw new InvalidOperationException($"CGGetOnlineDisplayList failed with code {result}.");
      }
    }

    if (count == 0)
    {
      throw new InvalidOperationException("No online displays were found; is a window server session active?");
    }

    var mainId = MacBindings.CGMainDisplayID();
    var displays = new List<DisplayInfo>();
    var seen = new HashSet<uint>();

    for (var i = 0; i < count; i++)
    {
      var displayId = ids[i];

      if (!seen.Add(displayId))
      {
        continue;
      }

      var bounds = MacBindings.CGDisplayBounds(displayId);
      var pixelsWide = (long)MacBindings.CGDisplayPixelsWide(displayId);
      var scale = bounds.Width > 0 && pixelsWide > 0 ? pixelsWide / bounds.Width : 1.0;

      displays.Add(new DisplayInfo
      {
        Index = displays.Count,
        Name = $"Display {displayId}",
        X = (int)Math.Round(bounds.X),
        Y = (int)Math.Round(bounds.Y),
        Width = (int)Math.Round(bounds.Width),
        Height = (int)Math.Round(bounds.Height),
        IsPrimary = displayId == mainId,
        Scale = scale,
      });
    }

    return new DisplayLayout(displays);
  }
}
