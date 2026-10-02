using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Bitbound.ComputerUseDotnet.ComputerUse.Linux;

/// <summary>
/// X11 backend: root-window capture via XGetImage and input via the XTEST extension.
/// The X server itself enforces access (same-user / local X connections only).
/// </summary>
internal sealed class X11Backend(ILogger<X11Backend> logger) : ComputerUseBackendBase
{
  private const int ZPixmap = 2;
  private const nuint AllPlanes = 0xFFFFFFFF;

  private static readonly Lock _sync = new();

  private readonly ILogger<X11Backend> _logger = logger;
  private nint _display;
  private nint _root;
  private uint _shiftKeycode;
  private bool _disposed;

  public override string BackendName => "Linux X11 (XGetImage capture + XTEST input)";

  public override DesktopEnvironmentType EnvironmentType => DesktopEnvironmentType.X11;

  public override unsafe Task<SKBitmap> CaptureVirtualScreenAsync(CancellationToken cancellationToken = default)
  {
    lock (_sync)
    {
      EnsureDisplay();

      var layout = BuildLayout();
      var image = LibX11.XGetImage(
        _display,
        _root,
        layout.OriginX,
        layout.OriginY,
        (uint)layout.Width,
        (uint)layout.Height,
        AllPlanes,
        ZPixmap);

      if (image == nint.Zero)
      {
        throw new InvalidOperationException("XGetImage failed to capture the root window.");
      }

      try
      {
        var ximage = Marshal.PtrToStructure<LibX11.XImage>(image);
        var width = ximage.width;
        var height = ximage.height;

        if (ximage.data == nint.Zero || width <= 0 || height <= 0)
        {
          throw new InvalidOperationException("XGetImage returned an empty image.");
        }

        // X11 root visuals carry no alpha (depth 24 in a 32bpp pixel); Opaque matches Windows' BGRA capture.
      var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
        var destination = bitmap.GetPixels();

        if (destination == nint.Zero)
        {
          bitmap.Dispose();
          throw new InvalidOperationException("Failed to allocate the capture bitmap.");
        }

        var source = (byte*)ximage.data;
        var target = (byte*)destination;

        for (var y = 0; y < height; y++)
        {
          var sourceRow = source + ((long)y * ximage.bytesPerLine);
          var destinationRow = target + ((long)y * bitmap.RowBytes);

          if (ximage.bitsPerPixel == 32)
          {
            Buffer.MemoryCopy(sourceRow, destinationRow, bitmap.RowBytes, (long)width * 4);
          }
          else if (ximage.bitsPerPixel == 24)
          {
            // X11 24bpp TrueColor is little-endian BGR in memory; widen to BGRA.
            for (var x = 0; x < width; x++)
            {
              destinationRow[x * 4 + 0] = sourceRow[x * 3 + 0];
              destinationRow[x * 4 + 1] = sourceRow[x * 3 + 1];
              destinationRow[x * 4 + 2] = sourceRow[x * 3 + 2];
              destinationRow[x * 4 + 3] = 0xFF;
            }
          }
          else
          {
            bitmap.Dispose();
            throw new InvalidOperationException($"Unsupported XImage depth: {ximage.bitsPerPixel} bits per pixel.");
          }
        }

        return Task.FromResult(bitmap);
      }
      finally
      {
        LibX11.XDestroyImage(image);
      }
    }
  }

  public override Task<DisplayLayout> GetDisplayLayoutAsync(CancellationToken cancellationToken = default)
  {
    lock (_sync)
    {
      EnsureDisplay();
      return Task.FromResult(BuildLayout());
    }
  }

  public override Task MovePointerAsync(ScreenPoint point, CancellationToken cancellationToken = default)
  {
    var native = PointToNative(point);

    lock (_sync)
    {
      EnsureDisplay();
      LibXtst.XTestFakeMotionEvent(_display, -1, native.X, native.Y, LibX11.CurrentTime);
      LibX11.XFlush(_display);
    }

    return Task.CompletedTask;
  }

  public override Task SetPointerButtonAsync(MouseButton button, bool pressed, CancellationToken cancellationToken = default)
  {
    var xButton = ToXButton(button);

    lock (_sync)
    {
      EnsureDisplay();
      LibXtst.XTestFakeButtonEvent(_display, xButton, pressed, LibX11.CurrentTime);
      LibX11.XFlush(_display);
    }

    return Task.CompletedTask;
  }

  public override async Task ScrollAsync(ScreenPoint point, int verticalClicks, int horizontalClicks, CancellationToken cancellationToken = default)
  {
    await MovePointerAsync(point, cancellationToken);

    lock (_sync)
    {
      EnsureDisplay();

      // X old-style wheel: button 4=up, 5=down, 6=left, 7=right; one press/release per click.
      FakeButtonRepeats(4, verticalClicks > 0 ? verticalClicks : 0);
      FakeButtonRepeats(5, verticalClicks < 0 ? -verticalClicks : 0);
      FakeButtonRepeats(6, horizontalClicks < 0 ? -horizontalClicks : 0);
      FakeButtonRepeats(7, horizontalClicks > 0 ? horizontalClicks : 0);

      LibX11.XFlush(_display);
    }
  }

  public override Task TypeTextAsync(string text, CancellationToken cancellationToken = default)
  {
    var skipped = 0;

    lock (_sync)
    {
      EnsureDisplay();

      foreach (var character in text)
      {
        cancellationToken.ThrowIfCancellationRequested();

        if (character is '\r')
        {
          continue;
        }

        if (!X11Keysyms.TryGetCharacterKeysymName(character, out var keysymName) ||
            !TryMapKeysymName(keysymName, out var keycode))
        {
          skipped++;
          continue;
        }

        var shifted = X11Keysyms.CharacterRequiresShift(character);

        if (shifted)
        {
          FakeKey(_shiftKeycode, true);
        }

        FakeKey(keycode, true);
        FakeKey(keycode, false);

        if (shifted)
        {
          FakeKey(_shiftKeycode, false);
        }
      }

      LibX11.XFlush(_display);
    }

    if (skipped > 0)
    {
      _logger.LogWarning("Skipped {Count} characters while typing because no X keysym mapping was available.", skipped);
    }

    if (skipped == text.Length && text.Length > 0)
    {
      throw new InvalidOperationException("No characters could be typed; the X server has no keysym mappings for the requested text.");
    }

    return Task.CompletedTask;
  }

  public override Task PressChordAsync(KeyChord chord, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();

    lock (_sync)
    {
      EnsureDisplay();

      var pressedKeycodes = new List<uint>();

      try
      {
        foreach (var modifier in chord.Modifiers)
        {
          pressedKeycodes.Add(PressKeysymName(X11Keysyms.GetModifierKeysymName(modifier)));
        }

        var shifted = false;
        uint targetKeycode;

        if (chord.Target.Character is { } character)
        {
          if (!X11Keysyms.TryGetCharacterKeysymName(character, out var keysymName) ||
              !TryMapKeysymName(keysymName, out targetKeycode))
          {
            throw new InvalidOperationException($"Character '{character}' has no usable X keysym mapping.");
          }

          shifted = X11Keysyms.CharacterRequiresShift(character);
        }
        else if (chord.Target.Name is { } name)
        {
          // Resolve without pressing; the single press/release below owns the target key events.
          if (!X11Keysyms.TryGetKeysymName(name, out var nameKeysymName) ||
              !TryMapKeysymName(nameKeysymName, out targetKeycode))
          {
            throw new InvalidOperationException($"Key '{name}' has no usable X keysym mapping.");
          }
        }
        else
        {
          throw new InvalidOperationException("Key chord has no target key.");
        }

        if (shifted)
        {
          FakeKey(_shiftKeycode, true);
          pressedKeycodes.Add(_shiftKeycode);
        }

        FakeKey(targetKeycode, true);
        FakeKey(targetKeycode, false);
      }
      finally
      {
        // Release in reverse press order so modifiers are held until the target is released.
        for (var i = pressedKeycodes.Count - 1; i >= 0; i--)
        {
          LibXtst.XTestFakeKeyEvent(_display, pressedKeycodes[i], false, LibX11.CurrentTime);
        }

        LibX11.XFlush(_display);
      }
    }

    return Task.CompletedTask;
  }

  public override Task<ScreenPoint?> GetCursorPositionAsync(CancellationToken cancellationToken = default)
  {
    lock (_sync)
    {
      EnsureDisplay();

      if (LibX11.XQueryPointer(_display, _root, out _, out _, out var rootX, out var rootY, out _, out _, out _) == 0)
      {
        return Task.FromResult<ScreenPoint?>(null);
      }

      var layout = BuildLayout();

      return Task.FromResult<ScreenPoint?>(new ScreenPoint(rootX - layout.OriginX, rootY - layout.OriginY));
    }
  }

  public override Task<PermissionStatus> CheckPermissionsAsync(CancellationToken cancellationToken = default)
  {
    lock (_sync)
    {
      EnsureDisplay();

      var xtestAvailable = LibXtst.XTestQueryExtension(_display, out _, out _, out _, out _);

      return Task.FromResult(new PermissionStatus(
        BackendName,
        PermissionState.Granted,
        xtestAvailable ? PermissionState.Granted : PermissionState.NotGranted,
        xtestAvailable
          ? "X11 clients are authorized by the X server itself (same-user/local connections); the XTEST extension is available."
          : "The XTEST extension is not available on this X server, so input simulation will fail."));
    }
  }

  public override Task<PermissionStatus> RequestPermissionsAsync(CancellationToken cancellationToken = default) =>
    CheckPermissionsAsync(cancellationToken);

  protected override void DisposeCore()
  {
    lock (_sync)
    {
      if (_disposed)
      {
        return;
      }

      _disposed = true;

      if (_display != nint.Zero)
      {
        LibX11.XCloseDisplay(_display);
        _display = nint.Zero;
      }
    }
  }

  private static uint ToXButton(MouseButton button) =>
    button switch
    {
      MouseButton.Left => 1,
      MouseButton.Middle => 2,
      MouseButton.Right => 3,
      MouseButton.Extra => 8,
      MouseButton.Side => 9,
      _ => throw new ArgumentOutOfRangeException(nameof(button), button, null),
    };

  private static ScreenPoint PointToNative(ScreenPoint point)
  {
    // The X11 root window always starts at (0,0), so normalized coordinates are native coordinates.
    return point;
  }

  private void FakeButtonRepeats(uint button, int count)
  {
    for (var i = 0; i < count; i++)
    {
      LibXtst.XTestFakeButtonEvent(_display, button, true, LibX11.CurrentTime);
      LibXtst.XTestFakeButtonEvent(_display, button, false, LibX11.CurrentTime);
    }
  }

  private void FakeKey(uint keycode, bool pressed) =>
    LibXtst.XTestFakeKeyEvent(_display, keycode, pressed, LibX11.CurrentTime);

  private bool TryMapKeysymName(string keysymName, out uint keycode)
  {
    var keysym = LibX11.XStringToKeysym(keysymName);
    keycode = keysym == nint.Zero ? 0 : LibX11.XKeysymToKeycode(_display, keysym);

    return keycode != 0;
  }

  private uint PressKeysymName(string keysymName)
  {
    var keysym = LibX11.XStringToKeysym(keysymName);

    if (keysym == nint.Zero)
    {
      throw new InvalidOperationException($"Unknown X keysym name '{keysymName}'.");
    }

    var keycode = LibX11.XKeysymToKeycode(_display, keysym);

    if (keycode == 0)
    {
      throw new InvalidOperationException($"X keysym '{keysymName}' has no keycode on this keyboard layout.");
    }

    LibXtst.XTestFakeKeyEvent(_display, keycode, true, LibX11.CurrentTime);
    return keycode;
  }

  private void EnsureDisplay()
  {
    if (_disposed)
    {
      throw new ObjectDisposedException(nameof(X11Backend));
    }

    if (_display != nint.Zero)
    {
      return;
    }

    LibX11.XInitThreads();

    _display = LibX11.XOpenDisplay(null);

    if (_display == nint.Zero)
    {
      throw new InvalidOperationException("XOpenDisplay failed: is DISPLAY set and is the X server reachable?");
    }

    _root = LibX11.XDefaultRootWindow(_display);
    _shiftKeycode = LibX11.XKeysymToKeycode(_display, LibX11.XStringToKeysym("Shift_L"));

    if (!LibXtst.XTestQueryExtension(_display, out _, out _, out _, out _))
    {
      _logger.LogWarning("The X server lacks the XTEST extension; input simulation will fail.");
    }
  }

  private DisplayLayout BuildLayout()
  {
    var screen = LibX11.XDefaultScreenOfDisplay(_display);
    var width = LibX11.XWidthOfScreen(screen);
    var height = LibX11.XHeightOfScreen(screen);

    // With Xinerama (single protocol screen spanning all monitors) the root window covers the whole desktop.
    return new DisplayLayout(
    [
      new DisplayInfo
      {
        Index = 0,
        Name = "X11 root",
        X = 0,
        Y = 0,
        Width = width,
        Height = height,
        IsPrimary = true,
        Scale = 1,
      },
    ]);
  }
}
