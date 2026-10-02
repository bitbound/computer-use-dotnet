using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Bitbound.ComputerUseDotnet.ComputerUse.Windows;

/// <summary>Windows capture via GDI BitBlt; input via SendInput. No user permission is required.</summary>
internal sealed class WindowsBackend(ILogger<WindowsBackend> logger) : ComputerUseBackendBase
{
  private const uint BiRgb = 0;

  private static readonly object DpiLock = new();

  private static bool _dpiAwarenessAttempted;

  private readonly ILogger<WindowsBackend> _logger = logger;

  static WindowsBackend()
  {
    EnsureDpiAwareness();
  }

  public override string BackendName => "Windows (GDI BitBlt + SendInput)";
  public override DesktopEnvironmentType EnvironmentType => DesktopEnvironmentType.Windows;

  public override unsafe Task<SKBitmap> CaptureVirtualScreenAsync(CancellationToken cancellationToken = default)
  {
    EnsureDpiAwareness();

    var x = Win32.GetSystemMetrics(Win32.SM_XVIRTUALSCREEN);
    var y = Win32.GetSystemMetrics(Win32.SM_YVIRTUALSCREEN);
    var width = Win32.GetSystemMetrics(Win32.SM_CXVIRTUALSCREEN);
    var height = Win32.GetSystemMetrics(Win32.SM_CYVIRTUALSCREEN);

    if (width <= 0 || height <= 0)
    {
      throw new InvalidOperationException("No virtual screen is available; is a desktop session active?");
    }

    var screenDc = Win32.GetDC(nint.Zero);

    if (screenDc == nint.Zero)
    {
      throw new InvalidOperationException("GetDC(NULL) failed; cannot access the screen device context.");
    }

    var memoryDc = Win32.CreateCompatibleDC(screenDc);
    var bitmapHandle = Win32.CreateCompatibleBitmap(screenDc, width, height);
    var originalObject = nint.Zero;

    try
    {
      if (memoryDc == nint.Zero || bitmapHandle == nint.Zero)
      {
        throw new InvalidOperationException("Failed to create the capture memory DC/bitmap.");
      }

      originalObject = Win32.SelectObject(memoryDc, bitmapHandle);

      if (!Win32.BitBlt(memoryDc, 0, 0, width, height, screenDc, x, y, Win32.SRCCOPY | Win32.CAPTUREBLT))
      {
        throw new InvalidOperationException($"BitBlt failed with Win32 error {Marshal.GetLastWin32Error()}.");
      }

      var info = default(Win32.BITMAPINFO);
      info.Header.Size = (uint)Marshal.SizeOf<Win32.BITMAPINFOHEADER>();
      info.Header.Width = width;
      info.Header.Height = -height; // top-down rows
      info.Header.Planes = 1;
      info.Header.BitCount = 32;
      info.Header.Compression = BiRgb;

      var buffer = new byte[width * height * 4];
      int lines;

      fixed (byte* bufferPointer = buffer)
      {
        lines = Win32.GetDIBits(screenDc, bitmapHandle, 0, (uint)height, bufferPointer, ref info, Win32.DIB_RGB_COLORS);
      }

      if (lines != height)
      {
        throw new InvalidOperationException($"GetDIBits returned {lines} of {height} scan lines.");
      }

      var result = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);

      fixed (byte* bufferPointer = buffer)
      {
        var pixels = result.GetPixels();

        if (pixels == nint.Zero)
        {
          result.Dispose();
          throw new InvalidOperationException("Failed to allocate bitmap pixels.");
        }

        Buffer.MemoryCopy(bufferPointer, pixels.ToPointer(), result.ByteCount, buffer.Length);
      }

      return Task.FromResult(result);
    }
    finally
    {
      if (originalObject != nint.Zero)
      {
        Win32.SelectObject(memoryDc, originalObject);
      }

      if (bitmapHandle != nint.Zero)
      {
        Win32.DeleteObject(bitmapHandle);
      }

      if (memoryDc != nint.Zero)
      {
        Win32.DeleteDC(memoryDc);
      }

      Win32.ReleaseDC(nint.Zero, screenDc);
    }
  }

  public override Task<PermissionStatus> CheckPermissionsAsync(CancellationToken cancellationToken = default) =>
    Task.FromResult(new PermissionStatus(
      BackendName,
      PermissionState.NotRequired,
      PermissionState.NotRequired,
      "Windows does not require special permissions for GDI capture or SendInput in a normal interactive session."));

  public override Task<ScreenPoint?> GetCursorPositionAsync(CancellationToken cancellationToken = default)
  {
    if (!Win32.GetCursorPos(out var point))
    {
      return Task.FromResult<ScreenPoint?>(null);
    }

    // GetCursorPos returns native virtual-screen coordinates; re-normalize to the union origin.
    var layout = GetDisplayLayoutAsync().GetAwaiter().GetResult();
    return Task.FromResult<ScreenPoint?>(layout.FromNative(new ScreenPoint(point.X, point.Y)));
  }

  public override unsafe Task<DisplayLayout> GetDisplayLayoutAsync(CancellationToken cancellationToken = default)
  {
    EnsureDpiAwareness();

    var displays = new List<DisplayInfo>();
    var index = 0;

    Win32.MonitorEnumProc callback = (hMonitor, _, lprcMonitor, _) =>
    {
      var info = new Win32.MONITORINFOEX
      {
        Size = (uint)Marshal.SizeOf<Win32.MONITORINFOEX>(),
      };

      if (!Win32.GetMonitorInfo(hMonitor, ref info))
      {
        return true;
      }

      var rect = info.Monitor;
      var scale = 1.0;

      try
      {
        if (Win32.GetDpiForMonitor(hMonitor, 0, out var dpiX, out _) == 0)
        {
          scale = dpiX / 96.0;
        }
      }
      catch (DllNotFoundException)
      {
        _logger.LogDebug("shcore.dll not available; assuming scale 1.0 for monitor {Monitor}.", info.DeviceName);
      }

      displays.Add(new DisplayInfo
      {
        Index = index++,
        Name = info.DeviceName,
        X = rect.Left,
        Y = rect.Top,
        Width = rect.Right - rect.Left,
        Height = rect.Bottom - rect.Top,
        IsPrimary = (info.Flags & Win32.MONITORINFOF_PRIMARY) != 0,
        Scale = scale,
      });

      return true;
    };

    if (!Win32.EnumDisplayMonitors(nint.Zero, null, callback, nint.Zero) || displays.Count == 0)
    {
      throw new InvalidOperationException("EnumDisplayMonitors returned no displays; is a desktop session active?");
    }

    return Task.FromResult(new DisplayLayout(displays));
  }

  public override Task MovePointerAsync(ScreenPoint point, CancellationToken cancellationToken = default)
  {
    // Normalized coordinates are relative to the virtual-screen origin, which is
    // (SM_XVIRTUALSCREEN, SM_YVIRTUALSCREEN) and can be negative with monitors left of primary.
    var native = ToNative(point);

    if (!Win32.SetCursorPos(native.X, native.Y))
    {
      throw new InvalidOperationException($"SetCursorPos({native.X}, {native.Y}) failed with Win32 error {Marshal.GetLastWin32Error()}.");
    }

    return Task.CompletedTask;
  }

  public override async Task PressChordAsync(KeyChord chord, CancellationToken cancellationToken = default)
  {
    var down = new List<(ushort VirtualKey, bool Extended)>();

    foreach (var modifier in chord.Modifiers)
    {
      down.Add((WindowsVirtualKeys.GetModifierVirtualKey(modifier), false));
    }

    switch (chord.Target)
    {
      case { Character: var character } when character is not null:
        break;

      case { Name: var name } when name is not null:
        if (!WindowsVirtualKeys.TryGetVirtualKey(name, out var virtualKey))
        {
          throw new InvalidOperationException($"Key '{name}' has no Windows virtual-key mapping.");
        }

        down.Add((virtualKey, WindowsVirtualKeys.IsExtended(name)));
        break;
    }

    foreach (var (virtualKey, extended) in down)
    {
      await SendKeyAsync(virtualKey, extended, pressed: true, cancellationToken);
    }

    if (chord.Target.Character is { } targetCharacter)
    {
      await SendUnicodeCharAsync(targetCharacter, pressed: true, cancellationToken);
      await SendUnicodeCharAsync(targetCharacter, pressed: false, cancellationToken);
    }

    for (var i = down.Count - 1; i >= 0; i--)
    {
      await SendKeyAsync(down[i].VirtualKey, down[i].Extended, pressed: false, cancellationToken);
    }
  }

  public override Task<PermissionStatus> RequestPermissionsAsync(CancellationToken cancellationToken = default) =>
    CheckPermissionsAsync(cancellationToken);

  public override async Task ScrollAsync(ScreenPoint point, int verticalClicks, int horizontalClicks, CancellationToken cancellationToken = default)
  {
    await MovePointerAsync(point, cancellationToken);

    if (verticalClicks != 0)
    {
      SendWheel(Win32.MOUSEEVENTF_WHEEL, verticalClicks * Win32.WHEEL_DELTA);
    }

    if (horizontalClicks != 0)
    {
      SendWheel(Win32.MOUSEEVENTF_HWHEEL, horizontalClicks * Win32.WHEEL_DELTA);
    }
  }

  public override Task SetPointerButtonAsync(MouseButton button, bool pressed, CancellationToken cancellationToken = default)
  {
    var (downFlag, upFlag, mouseData) = button switch
    {
      MouseButton.Left => (Win32.MOUSEEVENTF_LEFTDOWN, Win32.MOUSEEVENTF_LEFTUP, 0u),
      MouseButton.Right => (Win32.MOUSEEVENTF_RIGHTDOWN, Win32.MOUSEEVENTF_RIGHTUP, 0u),
      MouseButton.Middle => (Win32.MOUSEEVENTF_MIDDLEDOWN, Win32.MOUSEEVENTF_MIDDLEUP, 0u),
      MouseButton.Extra => (Win32.MOUSEEVENTF_XDOWN, Win32.MOUSEEVENTF_XUP, 1u),
      MouseButton.Side => (Win32.MOUSEEVENTF_XDOWN, Win32.MOUSEEVENTF_XUP, 2u),
      _ => throw new ArgumentOutOfRangeException(nameof(button), button, null),
    };

    var input = new Win32.INPUT
    {
      Type = Win32.INPUT_MOUSE,
      Union = new Win32.InputUnion
      {
        Mouse = new Win32.MOUSEINPUT
        {
          MouseData = mouseData,
          Flags = pressed ? downFlag : upFlag,
        },
      },
    };

    if (Win32.SendInputs(input) != 1)
    {
      throw new InvalidOperationException($"SendInput(mouse) failed with Win32 error {Marshal.GetLastWin32Error()}.");
    }

    return Task.CompletedTask;
  }

  public override async Task TypeTextAsync(string text, CancellationToken cancellationToken = default)
  {
    foreach (var character in text)
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (character is '\n' or '\r')
      {
        await SendKeyAsync(WindowsVirtualKeys.VK_RETURN, extended: false, pressed: true, cancellationToken);
        await SendKeyAsync(WindowsVirtualKeys.VK_RETURN, extended: false, pressed: false, cancellationToken);
        continue;
      }

      await SendUnicodeCharAsync(character, pressed: true, cancellationToken);
      await SendUnicodeCharAsync(character, pressed: false, cancellationToken);
    }
  }

  protected override void DisposeCore()
  {
  }

  private static void EnsureDpiAwareness()
  {
    lock (DpiLock)
    {
      if (_dpiAwarenessAttempted)
      {
        return;
      }

      _dpiAwarenessAttempted = true;
      Win32.TryEnablePerMonitorDpiAwareness();
    }
  }

  private static void SendWheel(uint flag, int delta)
  {
    var input = new Win32.INPUT
    {
      Type = Win32.INPUT_MOUSE,
      Union = new Win32.InputUnion
      {
        Mouse = new Win32.MOUSEINPUT
        {
          MouseData = unchecked((uint)delta),
          Flags = flag,
        },
      },
    };

    if (Win32.SendInputs(input) != 1)
    {
      throw new InvalidOperationException($"SendInput(wheel) failed with Win32 error {Marshal.GetLastWin32Error()}.");
    }
  }

  private static ScreenPoint ToNative(ScreenPoint point) =>
    new(point.X + Win32.GetSystemMetrics(Win32.SM_XVIRTUALSCREEN), point.Y + Win32.GetSystemMetrics(Win32.SM_YVIRTUALSCREEN));

  private Task SendKeyAsync(ushort virtualKey, bool extended, bool pressed, CancellationToken cancellationToken)
  {
    uint flags = 0;

    if (extended)
    {
      flags |= Win32.KEYEVENTF_EXTENDEDKEY;
    }

    if (!pressed)
    {
      flags |= Win32.KEYEVENTF_KEYUP;
    }

    var input = new Win32.INPUT
    {
      Type = Win32.INPUT_KEYBOARD,
      Union = new Win32.InputUnion
      {
        Keyboard = new Win32.KEYBDINPUT
        {
          VirtualKey = virtualKey,
          Flags = flags,
        },
      },
    };

    if (Win32.SendInputs(input) != 1)
    {
      throw new InvalidOperationException($"SendInput(key) failed with Win32 error {Marshal.GetLastWin32Error()}.");
    }

    return Task.CompletedTask;
  }

  private Task SendUnicodeCharAsync(char character, bool pressed, CancellationToken cancellationToken)
  {
    var flags = Win32.KEYEVENTF_UNICODE;

    if (!pressed)
    {
      flags |= Win32.KEYEVENTF_KEYUP;
    }

    var input = new Win32.INPUT
    {
      Type = Win32.INPUT_KEYBOARD,
      Union = new Win32.InputUnion
      {
        Keyboard = new Win32.KEYBDINPUT
        {
          VirtualKey = 0,
          Scan = character,
          Flags = flags,
        },
      },
    };

    if (Win32.SendInputs(input) != 1)
    {
      throw new InvalidOperationException($"SendInput(unicode) failed with Win32 error {Marshal.GetLastWin32Error()}.");
    }

    return Task.CompletedTask;
  }
}
