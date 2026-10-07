using Bitbound.ComputerUseDotnet.ComputerUse;

namespace Bitbound.ComputerUseDotnet.Tests;

/// <summary>Tests for Linux session detection via environment variables.</summary>
public class DesktopEnvironmentDetectorTests
{
  private static DesktopEnvironmentType Detect(Dictionary<string, string?> env) =>
    DesktopEnvironmentDetector.DetectLinuxSession(name => env.GetValueOrDefault(name));

  [Fact]
  public void WaylandDisplay_Present_Wayland()
  {
    Assert.Equal(DesktopEnvironmentType.Wayland, Detect(new() { ["WAYLAND_DISPLAY"] = "wayland-0" }));
  }

  [Fact]
  public void DisplayOnly_X11()
  {
    Assert.Equal(DesktopEnvironmentType.X11, Detect(new() { ["DISPLAY"] = ":0" }));
  }

  [Fact]
  public void XWaylandClient_BothVarsWithWaylandSessionType_Wayland()
  {
    Assert.Equal(
      DesktopEnvironmentType.Wayland,
      Detect(new() { ["DISPLAY"] = ":0", ["XDG_SESSION_TYPE"] = "wayland" }));
  }

  [Fact]
  public void SessionTypeX11_X11()
  {
    Assert.Equal(DesktopEnvironmentType.X11, Detect(new() { ["XDG_SESSION_TYPE"] = "x11" }));
  }

  [Fact]
  public void NoHints_Unknown()
  {
    Assert.Equal(DesktopEnvironmentType.Unknown, Detect(new()));
  }
}
