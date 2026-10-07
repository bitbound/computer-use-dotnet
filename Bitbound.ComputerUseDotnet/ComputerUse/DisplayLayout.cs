namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>
/// The current display layout: all displays plus the union ("virtual screen") bounds that
/// MCP tools map position fractions onto.
/// </summary>
public sealed class DisplayLayout
{
  public DisplayLayout(IReadOnlyList<DisplayInfo> displays)
  {
    ArgumentNullException.ThrowIfNull(displays);
    if (displays.Count == 0)
    {
      throw new ArgumentException("At least one display is required.", nameof(displays));
    }

    OriginX = displays.Min(d => d.X);
    OriginY = displays.Min(d => d.Y);
    var maxRight = displays.Max(d => d.Right);
    var maxBottom = displays.Max(d => d.Bottom);
    Width = maxRight - OriginX;
    Height = maxBottom - OriginY;

    // Re-stamp each display with the virtual-screen origin so it can convert points itself.
    Displays = displays
      .Select(d => d with { OriginX = OriginX, OriginY = OriginY })
      .ToList();
  }

  public IReadOnlyList<DisplayInfo> Displays { get; }

  /// <summary>Height of the union of all displays in logical pixels.</summary>
  public int Height { get; }

  /// <summary>Native X of the normalized origin.</summary>
  public int OriginX { get; }

  /// <summary>Native Y of the normalized origin.</summary>
  public int OriginY { get; }
  public DisplayInfo? Primary =>
    Displays.FirstOrDefault(d => d.IsPrimary) ?? Displays.FirstOrDefault();

  /// <summary>Width of the union of all displays in logical pixels.</summary>
  public int Width { get; }

  /// <summary>Clamps a virtual-screen point to the virtual screen bounds.</summary>
  public ScreenPoint Clamp(ScreenPoint point) => new(
    Math.Clamp(point.X, 0, Math.Max(0, Width - 1)),
    Math.Clamp(point.Y, 0, Math.Max(0, Height - 1)));

  /// <summary>Finds the display whose bounds contain the given virtual-screen point.</summary>
  public DisplayInfo? DisplayAt(ScreenPoint virtualScreenPoint) =>
    Displays.FirstOrDefault(d =>
      virtualScreenPoint.X >= d.X - OriginX &&
      virtualScreenPoint.X < d.Right - OriginX &&
      virtualScreenPoint.Y >= d.Y - OriginY &&
      virtualScreenPoint.Y < d.Bottom - OriginY);

  /// <summary>
  /// The fraction range of the virtual screen that one display covers. This is the conversion a
  /// caller needs to turn a fraction inside a single-display capture into a whole-desktop fraction.
  /// </summary>
  public (NormalizedPoint Min, NormalizedPoint Max) FractionBoundsOf(DisplayInfo display)
  {
    ArgumentNullException.ThrowIfNull(display);

    var spanX = Math.Max(1, Width - 1);
    var spanY = Math.Max(1, Height - 1);

    return (
      new NormalizedPoint(
        (display.X - OriginX) / (double)spanX,
        (display.Y - OriginY) / (double)spanY),
      new NormalizedPoint(
        (display.Right - OriginX - 1) / (double)spanX,
        (display.Bottom - OriginY - 1) / (double)spanY));
  }

  /// <summary>Converts native layout coordinates into virtual-screen space.</summary>
  public ScreenPoint FromNative(ScreenPoint point) => new(point.X - OriginX, point.Y - OriginY);

  /// <summary>
  /// Converts a fraction of the virtual screen into a virtual-screen point. Clamped so that 1.0
  /// lands on the last pixel column and row instead of just past the edge.
  /// </summary>
  public ScreenPoint FromNormalized(NormalizedPoint fraction) =>
    new(
      (int)Math.Round(Math.Max(0, Width - 1) * Math.Clamp(fraction.X, 0, 1), MidpointRounding.AwayFromZero),
      (int)Math.Round(Math.Max(0, Height - 1) * Math.Clamp(fraction.Y, 0, 1), MidpointRounding.AwayFromZero));

  /// <summary>Converts a virtual-screen point into native layout coordinates.</summary>
  public ScreenPoint ToNative(ScreenPoint point) => new(point.X + OriginX, point.Y + OriginY);

  /// <summary>Converts a virtual-screen point into a fraction of the virtual screen.</summary>
  public NormalizedPoint ToNormalized(ScreenPoint point)
  {
    var clamped = Clamp(point);

    return new NormalizedPoint(
      clamped.X / (double)Math.Max(1, Width - 1),
      clamped.Y / (double)Math.Max(1, Height - 1));
  }
}
