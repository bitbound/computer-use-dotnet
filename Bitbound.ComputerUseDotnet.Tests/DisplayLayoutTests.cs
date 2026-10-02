using Bitbound.ComputerUseDotnet.ComputerUse;

namespace Bitbound.ComputerUseDotnet.Tests;

/// <summary>Tests for <see cref="DisplayLayout"/> virtual-screen normalization.</summary>
public class DisplayLayoutTests
{
  private static DisplayLayout MakeTwoDisplayLayout() =>
    new(
    [
      new DisplayInfo
      {
        Index = 0,
        Name = "left",
        X = -1920,
        Y = 0,
        Width = 1920,
        Height = 1080,
        IsPrimary = false,
        Scale = 1,
      },
      new DisplayInfo
      {
        Index = 1,
        Name = "main",
        X = 0,
        Y = 0,
        Width = 2560,
        Height = 1440,
        IsPrimary = true,
        Scale = 2,
      },
    ]);

  [Fact]
  public void Layout_UnionCoversAllDisplays()
  {
    var layout = MakeTwoDisplayLayout();

    Assert.Equal(-1920, layout.OriginX);
    Assert.Equal(0, layout.OriginY);
    Assert.Equal(4480, layout.Width);
    Assert.Equal(1440, layout.Height);
  }

  [Fact]
  public void ToNativeFromNative_RoundTrips()
  {
    var layout = MakeTwoDisplayLayout();
    var point = new ScreenPoint(100, 50);

    var native = layout.ToNative(point);
    Assert.Equal(new ScreenPoint(-1820, 50), native);
    Assert.Equal(point, layout.FromNative(native));
  }

  [Fact]
  public void Clamp_KeepsPointsInsideVirtualScreen()
  {
    var layout = MakeTwoDisplayLayout();

    Assert.Equal(new ScreenPoint(0, 0), layout.Clamp(new ScreenPoint(-10, -10)));
    Assert.Equal(new ScreenPoint(4479, 1439), layout.Clamp(new ScreenPoint(99999, 99999)));
  }

  [Fact]
  public void DisplayAt_SelectsDisplayContainingPoint()
  {
    var layout = MakeTwoDisplayLayout();

    // Normalized (0,0) is the left edge of the left display.
    Assert.Equal("left", layout.DisplayAt(new ScreenPoint(0, 0))?.Name);

    // Normalized (1920, 10) is the left edge of the main display.
    Assert.Equal("main", layout.DisplayAt(new ScreenPoint(1920, 10))?.Name);
  }

  [Fact]
  public void Primary_PrefersIsPrimaryFlag()
  {
    var layout = MakeTwoDisplayLayout();

    Assert.Equal("main", layout.Primary?.Name);
  }
}
