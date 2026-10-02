using System.Runtime.InteropServices;

namespace Bitbound.ComputerUseDotnet.ComputerUse.Linux;

/// <summary>Minimal libX11 bindings for root-window capture, cursor query, and keysym lookup.</summary>
internal static class LibX11
{
  public const ulong CurrentTime = 0;

  private const string LibraryName = "libX11.so.6";

  [DllImport(LibraryName)]
  public static extern void XCloseDisplay(nint display);

  [DllImport(LibraryName)]
  public static extern nint XDefaultRootWindow(nint display);

  [DllImport(LibraryName)]
  public static extern nint XDefaultScreenOfDisplay(nint display);

  [DllImport(LibraryName)]
  public static extern void XDestroyImage(nint ximage);

  [DllImport(LibraryName)]
  public static extern void XFlush(nint display);

  [DllImport(LibraryName)]
  public static extern nint XGetImage(nint display, nint drawable, int x, int y, uint width, uint height, nuint planeMask, int format);

  [DllImport(LibraryName)]
  public static extern int XHeightOfScreen(nint screen);

  [DllImport(LibraryName)]
  public static extern int XInitThreads();

  [DllImport(LibraryName)]
  public static extern uint XKeysymToKeycode(nint display, nint keysym);

  [DllImport(LibraryName)]
  public static extern nint XOpenDisplay(string? displayName);

  [DllImport(LibraryName)]
  public static extern int XQueryPointer(
    nint display,
    nint window,
    out nint rootReturn,
    out nint childReturn,
    out int rootXReturn,
    out int rootYReturn,
    out int winXReturn,
    out int winYReturn,
    out uint maskReturn);

  [DllImport(LibraryName)]
  public static extern int XRootX(nint screen);

  [DllImport(LibraryName)]
  public static extern int XRootY(nint screen);

  [DllImport(LibraryName)]
  public static extern int XScreenCount(nint display);

  [DllImport(LibraryName)]
  public static extern nint XStringToKeysym(string key);

  [DllImport(LibraryName)]
  public static extern void XSync(nint display, bool discard);

  [DllImport(LibraryName)]
  public static extern int XWidthOfScreen(nint screen);

  [StructLayout(LayoutKind.Sequential)]
  public struct XImage
  {
    public int width;
    public int height;
    public int xoffset;
    public int format;
    public nint data;
    public int byteOrder;
    public int bitmapUnit;
    public int bitmapBitOrder;
    public int bitmapPad;
    public int depth;
    public int bytesPerLine;
    public int bitsPerPixel;
    public nuint redMask;
    public nuint greenMask;
    public nuint blueMask;
    public nint obdata;
  }
}
