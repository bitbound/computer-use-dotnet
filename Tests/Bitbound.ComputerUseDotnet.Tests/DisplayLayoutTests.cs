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

  [Fact]
  public void FromNormalized_CornersMapToFirstAndLastPixel()
  {
    var layout = MakeTwoDisplayLayout();

    Assert.Equal(new ScreenPoint(0, 0), layout.FromNormalized(new NormalizedPoint(0, 0)));
    Assert.Equal(new ScreenPoint(4479, 1439), layout.FromNormalized(new NormalizedPoint(1, 1)));
  }

  [Fact]
  public void FromNormalized_CenterRoundsToMiddlePixel()
  {
    var layout = MakeTwoDisplayLayout();

    Assert.Equal(new ScreenPoint(2240, 720), layout.FromNormalized(new NormalizedPoint(0.5, 0.5)));
  }

  [Fact]
  public void FromNormalized_ClampsFractionsOutsideUnitRange()
  {
    var layout = MakeTwoDisplayLayout();

    Assert.Equal(new ScreenPoint(4479, 0), layout.FromNormalized(new NormalizedPoint(2, -1)));
  }

  [Fact]
  public void Normalized_RoundTripsForSinglePixelLayout()
  {
    var layout = new DisplayLayout(
    [
      new DisplayInfo
      {
        Index = 0,
        Name = "tiny",
        X = 0,
        Y = 0,
        Width = 1,
        Height = 1,
        IsPrimary = true,
      },
    ]);

    Assert.Equal(new ScreenPoint(0, 0), layout.FromNormalized(new NormalizedPoint(0.5, 0.5)));
    Assert.Equal(new NormalizedPoint(0, 0), layout.ToNormalized(new ScreenPoint(0, 0)));
  }

  [Theory]
  [InlineData(0, 0)]
  [InlineData(1, 719)]
  [InlineData(4479, 1439)]
  [InlineData(2240, 720)]
  public void Normalized_RoundTripsOnTwoDisplayLayout(int x, int y)
  {
    var layout = MakeTwoDisplayLayout();
    var original = new ScreenPoint(x, y);

    Assert.Equal(original, layout.FromNormalized(layout.ToNormalized(original)));
  }

  [Fact]
  public void FractionBoundsOf_TwoDisplays_SplitUnitRangeAtDisplayEdges()
  {
    var layout = MakeTwoDisplayLayout();
    var left = layout.Displays.First(d => d.Name == "left");
    var main = layout.Displays.First(d => d.Name == "main");

    Assert.Equal(0, layout.FractionBoundsOf(left).Min.X, 9);
    Assert.Equal((double)1919 / 4479, layout.FractionBoundsOf(left).Max.X, 9);
    Assert.Equal((double)1920 / 4479, layout.FractionBoundsOf(main).Min.X, 9);
    Assert.Equal(1, layout.FractionBoundsOf(main).Max.X, 9);
    Assert.Equal(1, layout.FractionBoundsOf(main).Max.Y, 9);
  }
}
