namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>
/// The current display layout: all displays plus the union ("virtual screen") bounds
/// used as the normalized coordinate space for MCP tools.
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

  /// <summary>Clamps a normalized point to the virtual screen bounds.</summary>
  public ScreenPoint Clamp(ScreenPoint point) => new(
    Math.Clamp(point.X, 0, Math.Max(0, Width - 1)),
    Math.Clamp(point.Y, 0, Math.Max(0, Height - 1)));

  /// <summary>Finds the display whose normalized bounds contain the given normalized point.</summary>
  public DisplayInfo? DisplayAt(ScreenPoint normalizedPoint) =>
    Displays.FirstOrDefault(d =>
      normalizedPoint.X >= d.X - OriginX &&
      normalizedPoint.X < d.Right - OriginX &&
      normalizedPoint.Y >= d.Y - OriginY &&
      normalizedPoint.Y < d.Bottom - OriginY);

  /// <summary>Converts native layout coordinates into virtual-screen (normalized) space.</summary>
  public ScreenPoint FromNative(ScreenPoint point) => new(point.X - OriginX, point.Y - OriginY);

  /// <summary>Converts a virtual-screen (normalized) point into native layout coordinates.</summary>
  public ScreenPoint ToNative(ScreenPoint point) => new(point.X + OriginX, point.Y + OriginY);
}
