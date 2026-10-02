using System.Runtime.InteropServices;

namespace Bitbound.ComputerUseDotnet.ComputerUse.Linux;

/// <summary>libXtst XTEST extension bindings for input simulation on X11.</summary>
internal static class LibXtst
{
  private const string LibraryName = "libXtst.so.6";

  [DllImport(LibraryName)]
  public static extern void XTestFakeButtonEvent(nint display, uint button, bool isPress, ulong delay);

  [DllImport(LibraryName)]
  public static extern void XTestFakeKeyEvent(nint display, uint keycode, bool isPress, ulong delay);

  [DllImport(LibraryName)]
  public static extern void XTestFakeMotionEvent(nint display, int screenNumber, int x, int y, ulong delay);

  [DllImport(LibraryName)]
  public static extern bool XTestQueryExtension(nint display, out int eventBase, out int errorBase, out int majorVersion, out int minorVersion);
}
