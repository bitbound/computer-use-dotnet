using Bitbound.ComputerUseDotnet.ComputerUse;

namespace Bitbound.ComputerUseDotnet.Tests;

/// <summary>Tests for the fraction rules that MCP input tools apply to caller coordinates.</summary>
public class ScreenFractionTests
{
  private static DisplayLayout MakeLayout() =>
    new(
    [
      new DisplayInfo
      {
        Index = 0,
        Name = "main",
        X = 0,
        Y = 0,
        Width = 4480,
        Height = 1440,
        IsPrimary = true,
      },
    ]);

  [Theory]
  [InlineData(0.0, 0.0, 0, 0)]
  [InlineData(1.0, 1.0, 4479, 1439)]
  [InlineData(0.5, 0.5, 2240, 720)]
  [InlineData(0.625, 0.8, 2799, 1151)]
  public void TryResolvePoint_AcceptsFractions(double x, double y, int expectedX, int expectedY)
  {
    var resolved = ComputerUseTools.TryResolvePoint(x, y, MakeLayout(), out var point, out var rejection);

    Assert.True(resolved);
    Assert.Null(rejection);
    Assert.Equal(new ScreenPoint(expectedX, expectedY), point);
  }

  [Theory]
  [InlineData(1.0, 1.0, 4479, 1439)]
  [InlineData(625.0, 800.0, 2799, 1151)]
  [InlineData(500.0, 1000.0, 2240, 1439)]
  public void TryResolvePoint_ReadsValuesAboveOneAsThousandths(double x, double y, int expectedX, int expectedY)
  {
    var resolved = ComputerUseTools.TryResolvePoint(x, y, MakeLayout(), out var point, out var rejection);

    Assert.True(resolved);
    Assert.Null(rejection);
    Assert.Equal(new ScreenPoint(expectedX, expectedY), point);
  }

  [Fact]
  public void TryResolvePoint_PixelCoordinate_RejectsAndSuggestsTheFraction()
  {
    var resolved = ComputerUseTools.TryResolvePoint(2400, 10, MakeLayout(), out var point, out var rejection);

    Assert.False(resolved);
    Assert.Equal(new ScreenPoint(0, 0), point);
    Assert.NotNull(rejection);
    Assert.Contains("x=2400", rejection);
    Assert.Contains("4480x1440", rejection);
    Assert.Contains("0.536", rejection);
  }

  [Fact]
  public void TryResolvePoint_PixelBeyondTheDesktop_RejectsAndStatesThePixelRange()
  {
    var resolved = ComputerUseTools.TryResolvePoint(5000, 10, MakeLayout(), out _, out var rejection);

    Assert.False(resolved);
    Assert.NotNull(rejection);
    Assert.Contains("Pixels on this axis run 0 to 4479", rejection);
    Assert.DoesNotContain("is x=", rejection);
  }

  [Fact]
  public void TryResolvePoint_NegativeCoordinate_Rejects()
  {
    var resolved = ComputerUseTools.TryResolvePoint(0.5, -0.2, MakeLayout(), out _, out var rejection);

    Assert.False(resolved);
    Assert.NotNull(rejection);
    Assert.Contains("y=-0.2", rejection);
  }

  [Theory]
  [InlineData(double.NaN, 0.5)]
  [InlineData(0.5, double.PositiveInfinity)]
  public void TryResolvePoint_NonFiniteCoordinate_Rejects(double x, double y)
  {
    var resolved = ComputerUseTools.TryResolvePoint(x, y, MakeLayout(), out _, out var rejection);

    Assert.False(resolved);
    Assert.NotNull(rejection);
  }
}
