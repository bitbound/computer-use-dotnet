namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>
/// A single display, described in the platform's native layout coordinates
/// (logical pixels; on Windows with per-monitor DPI awareness these are physical pixels).
/// </summary>
public sealed record DisplayInfo
{
  /// <summary>Zero-based display index used by MCP tools.</summary>
  public required int Index { get; init; }

  /// <summary>Human readable display name.</summary>
  public required string Name { get; init; }

  /// <summary>X of the display's bounds in native layout coordinates.</summary>
  public required int X { get; init; }

  /// <summary>Y of the display's bounds in native layout coordinates.</summary>
  public required int Y { get; init; }

  /// <summary>Width of the display in logical pixels.</summary>
  public required int Width { get; init; }

  /// <summary>Height of the display in logical pixels.</summary>
  public required int Height { get; init; }

  /// <summary>Whether this is the primary display.</summary>
  public bool IsPrimary { get; init; }

  /// <summary>Physical pixels per logical pixel (informational; e.g. 2.0 on Retina).</summary>
  public double Scale { get; init; } = 1.0;

  public int Right => X + Width;

  public int Bottom => Y + Height;

  /// <summary>Converts a virtual-screen (normalized) point to this display's native coordinate space.</summary>
  public ScreenPoint ToNative(ScreenPoint point) => new(point.X + OriginX, point.Y + OriginY);

  /// <summary>Native X of the virtual-screen origin, i.e. the smallest display X.</summary>
  internal int OriginX { get; init; }

  /// <summary>Native Y of the virtual-screen origin, i.e. the smallest display Y.</summary>
  internal int OriginY { get; init; }
}
