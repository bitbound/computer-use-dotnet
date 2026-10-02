using System.Runtime.InteropServices;

namespace Bitbound.ComputerUseDotnet.ComputerUse.Mac;

/// <summary>Hand-written CoreGraphics/CoreFoundation/ApplicationServices bindings for macOS.</summary>
internal static unsafe class MacBindings
{
  public const int kCFStringEncodingUTF8 = 0x08000100;
  public const uint kCGEventKeyDown = 10;
  public const uint kCGEventKeyUp = 11;
  public const uint kCGEventLeftMouseDown = 1;
  public const uint kCGEventLeftMouseDragged = 6;
  public const uint kCGEventLeftMouseUp = 2;
  public const uint kCGEventMouseMoved = 5;
  public const uint kCGEventOtherMouseDown = 25;
  public const uint kCGEventOtherMouseUp = 26;
  public const uint kCGEventRightMouseDown = 3;
  public const uint kCGEventRightMouseDragged = 7;
  public const uint kCGEventRightMouseUp = 4;
  public const uint kCGEventScrollWheel = 22;
  public const int kCGEventSourceStateHIDSystemState = 1;
  public const uint kCGHIDEventTap = 0;
  public const uint kCGMouseButtonCenter = 2;
  public const uint kCGMouseButtonLeft = 0;
  public const uint kCGMouseButtonRight = 1;
  public const uint kCGMouseEventClickState = 1;
  public const uint kCGScrollEventUnitLine = 1;
  public const int kCGScrollWheelEventDeltaAxis1 = 11;
  public const uint kCGSessionEventTap = 1;

  private const string ApplicationServices = "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";
  private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
  private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
  private const string LibSystem = "/usr/lib/libSystem.B.dylib";
  private const int RTldLazy = 0x1;

  private static readonly object SymbolLock = new();

  private static nint _coreFoundationHandle = nint.Zero;

  /// <summary>Creates an AXIsProcessTrustedWithOptions option dictionary that prompts for Accessibility.</summary>
  public static unsafe nint CreateAccessibilityPromptOptions()
  {
    var key = CFStringCreateWithCString(nint.Zero, "AXTrustedCheckOptionPrompt", kCFStringEncodingUTF8);
    var value = ReadCoreFoundationConstant("kCFBooleanTrue");

    if (key == nint.Zero)
    {
      throw new InvalidOperationException("CFStringCreateWithCString failed for the Accessibility prompt key.");
    }

    var keys = stackalloc nint[1] { key };
    var values = stackalloc nint[1] { value };

    try
    {
      return CFDictionaryCreate(nint.Zero, keys, values, 1, nint.Zero, nint.Zero);
    }
    finally
    {
      CFRelease(key);
    }
  }

  /// <summary>Reads the value of a CoreFoundation data symbol (e.g. kCFBooleanTrue).</summary>
  public static nint ReadCoreFoundationConstant(string symbol)
  {
    lock (SymbolLock)
    {
      if (_coreFoundationHandle == nint.Zero)
      {
        _coreFoundationHandle = dlopen(CoreFoundation, RTldLazy);

        if (_coreFoundationHandle == nint.Zero)
        {
          throw new InvalidOperationException($"dlopen failed while loading CoreFoundation for '{symbol}'.");
        }
      }

      var address = dlsym(_coreFoundationHandle, symbol);

      if (address == nint.Zero)
      {
        throw new InvalidOperationException($"dlsym failed for CoreFoundation symbol '{symbol}'.");
      }

      return Marshal.ReadIntPtr(address);
    }
  }

  [DllImport(ApplicationServices)]
  public static extern bool AXIsProcessTrusted();

  [DllImport(ApplicationServices)]
  public static extern bool AXIsProcessTrustedWithOptions(nint options);

  [DllImport(CoreFoundation)]
  public static extern nint CFDataGetBytePtr(nint data);

  [DllImport(CoreFoundation)]
  public static extern nint CFDataGetLength(nint data);

  [DllImport(CoreFoundation)]
  public static extern nint CFDictionaryCreate(
    nint allocator,
    nint* keys,
    nint* values,
    nint count,
    nint keyCallbacks,
    nint valueCallbacks);

  // ---- CoreFoundation memory and option-dictionary helpers ----

  [DllImport(CoreFoundation)]
  public static extern void CFRelease(nint cf);

  [DllImport(CoreFoundation)]
  public static extern nint CFStringCreateWithCString(nint allocator, string value, int encoding);

  [DllImport(CoreGraphics)]
  public static extern nint CGDataProviderCopyData(nint provider);

  [DllImport(CoreGraphics)]
  public static extern CGRect CGDisplayBounds(uint display);

  [DllImport(CoreGraphics)]
  public static extern nint CGDisplayCreateImage(uint display);

  [DllImport(CoreGraphics)]
  public static extern nint CGDisplayPixelsHigh(uint display);

  [DllImport(CoreGraphics)]
  public static extern nint CGDisplayPixelsWide(uint display);

  [DllImport(CoreGraphics)]
  public static extern nint CGEventCreate(nint source);

  [DllImport(CoreGraphics)]
  public static extern nint CGEventCreateKeyboardEvent(nint source, ushort virtualKey, bool keyDown);

  [DllImport(CoreGraphics)]
  public static extern nint CGEventCreateMouseEvent(nint source, uint mouseType, CGPoint position, uint button);

  [DllImport(CoreGraphics)]
  public static extern nint CGEventCreateScrollWheelEvent(nint source, uint units, uint wheelCount, int wheel1, int wheel2);

  [DllImport(CoreGraphics)]
  public static extern CGPoint CGEventGetLocation(nint eventRef);

  // UniChar is UTF-16; LPWStr marshals the string as 2-byte code units.
  [DllImport(CoreGraphics)]
  public static extern void CGEventKeyboardSetUnicodeString(
    nint eventRef,
    nint length,
    [MarshalAs(UnmanagedType.LPWStr)] string unicodeString);

  [DllImport(CoreGraphics)]
  public static extern void CGEventPost(uint tap, nint eventRef);

  [DllImport(CoreGraphics)]
  public static extern void CGEventSetFlags(nint eventRef, ulong flags);

  [DllImport(CoreGraphics)]
  public static extern void CGEventSetIntegerValueField(nint eventRef, uint field, long value);

  [DllImport(CoreGraphics)]
  public static extern nint CGEventSourceCreate(uint stateID);

  // ---- CoreGraphics: displays and capture ----

  [DllImport(CoreGraphics)]
  public static extern int CGGetOnlineDisplayList(uint maxDisplays, uint[] displays, out uint count);

  [DllImport(CoreGraphics)]
  public static extern nint CGImageGetBitsPerPixel(nint image);

  [DllImport(CoreGraphics)]
  public static extern nint CGImageGetBytesPerRow(nint image);

  [DllImport(CoreGraphics)]
  public static extern nint CGImageGetDataProvider(nint image);

  [DllImport(CoreGraphics)]
  public static extern nint CGImageGetHeight(nint image);

  [DllImport(CoreGraphics)]
  public static extern nint CGImageGetWidth(nint image);

  [DllImport(CoreGraphics)]
  public static extern uint CGMainDisplayID();

  // ---- Permission APIs ----

  [DllImport(CoreGraphics)]
  public static extern bool CGPreflightScreenCaptureAccess();

  [DllImport(CoreGraphics)]
  public static extern bool CGRequestScreenCaptureAccess();

  // ---- CoreGraphics: cursor and events ----

  [DllImport(CoreGraphics)]
  public static extern int CGWarpMouseCursorPosition(CGPoint newCursorPosition);

  [DllImport(LibSystem)]
  private static extern nint dlopen(string path, int flags);

  [DllImport(LibSystem)]
  private static extern nint dlsym(nint handle, string symbol);

  [StructLayout(LayoutKind.Sequential)]
  public struct CGPoint
  {
    public double X;
    public double Y;

    public CGPoint(double x, double y)
    {
      X = x;
      Y = y;
    }
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct CGRect
  {
    public CGPoint Origin;
    public CGSize Size;

    public double X => Origin.X;

    public double Y => Origin.Y;

    public double Width => Size.Width;

    public double Height => Size.Height;

    public double Right => Origin.X + Size.Width;

    public double Bottom => Origin.Y + Size.Height;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct CGSize
  {
    public double Width;
    public double Height;
  }
}
