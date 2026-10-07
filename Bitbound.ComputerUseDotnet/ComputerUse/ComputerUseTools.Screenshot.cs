namespace Bitbound.ComputerUseDotnet.ComputerUse;

public sealed partial class ComputerUseTools
{
  [McpServerTool(Name = "take_screenshot")]
  [Description(
    "Captures the screen and returns it as a PNG image, followed by a text block of capture metadata. " +
    "When this captures the whole desktop, the input tools' fractions map straight onto it: x 0.0 is this " +
    "image's left edge and 1.0 its right edge, y 0.0 its top and 1.0 its bottom. Fractions name the same " +
    "spot at any image size, so downscaling never changes a coordinate.")]
  public async Task<IEnumerable<ContentBlock>> TakeScreenshot(
      [Description("Display index to capture (see get_desktop_info). -1 captures the entire virtual screen (all displays). Default: -1.")]
        int display = -1,
      [Description("Optionally downscale the returned image so its longest side is at most this many pixels (token saving). 0 keeps native resolution. Input coordinates are fractions, so they are unaffected by this.")]
        int max_side = 0)
  {
    using var bitmap = await _backend.CaptureVirtualScreenAsync();
    var layout = await _backend.GetDisplayLayoutAsync();

    SKBitmap? crop = null;
    var working = bitmap;
    if (display >= 0)
    {
      if (display >= layout.Displays.Count)
      {
        throw new ArgumentException($"Display index {display} is out of range (0..{layout.Displays.Count - 1}).");
      }

      var target = layout.Displays[display];
      var rect = new SKRectI(
        target.X - layout.OriginX,
        target.Y - layout.OriginY,
        target.Right - layout.OriginX,
        target.Bottom - layout.OriginY);

      rect.Intersect(new SKRectI(0, 0, bitmap.Width, bitmap.Height));

      if (rect.Width <= 0 || rect.Height <= 0)
      {
        throw new InvalidOperationException("The requested display lies outside the captured image.");
      }

      crop = new SKBitmap();
      bitmap.ExtractSubset(crop, rect);
      working = crop;
    }

    var sourceWidth = working.Width;
    var sourceHeight = working.Height;
    var scaled = max_side > 0 && Math.Max(sourceWidth, sourceHeight) > max_side;
    SKBitmap? resize = null;

    if (scaled)
    {
      var downscale = (double)max_side / Math.Max(sourceWidth, sourceHeight);
      var newWidth = Math.Max(1, (int)Math.Round(sourceWidth * downscale));
      var newHeight = Math.Max(1, (int)Math.Round(sourceHeight * downscale));

      resize = new SKBitmap(newWidth, newHeight);

      using (var resizeCanvas = new SKCanvas(resize))
      using (var sourceImage = SKImage.FromBitmap(working))
      {
        resizeCanvas.Clear(SKColors.Black);
        resizeCanvas.DrawImage(
          sourceImage,
          new SKRect(0, 0, newWidth, newHeight),
          new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
      }

      working = resize;
    }

    using var image = SKImage.FromBitmap(working);
    using var data = image.Encode(SKEncodedImageFormat.Png, 90);

    var text =
      $"Capture: {_backend.BackendName}. " +
      $"Image {working.Width}x{working.Height}px (virtual screen {layout.Width}x{layout.Height}px" +
      (display >= 0 ? $", display {display} crop {sourceWidth}x{sourceHeight}px" : ", full virtual screen") + "). " +
      (scaled ? "Downscaled to save tokens; input fractions are unaffected by resizing. " : string.Empty) +
      (display >= 0
        ? CropFractionNote(layout, layout.Displays[display])
        : "Input tools take fractions of this image: x 0.0 left edge to 1.0 right edge, y 0.0 top to 1.0 bottom.") +
      " Remember to call request_permissions if tools report missing permissions.";

    var blocks = new List<ContentBlock>
    {
      new TextContentBlock { Text = text },
      ImageContentBlock.FromBytes(data.ToArray(), "image/png"),
    };

    crop?.Dispose();
    resize?.Dispose();

    return blocks;
  }

  /// <summary>
  /// Tells a caller how to turn a fraction inside a single-display capture into a whole-desktop fraction.
  /// The mapping stays linear only because a crop is a plain subset with no padding or letterboxing.
  /// </summary>
  private static string CropFractionNote(DisplayLayout layout, DisplayInfo display)
  {
    var (min, max) = layout.FractionBoundsOf(display);
    var minX = Format(min.X);
    var maxX = Format(max.X);
    var minY = Format(min.Y);
    var maxY = Format(max.Y);

    return
      $"This image is one display, not the whole desktop. It covers desktop x {minX} to {maxX} and y {minY} to {maxY}. " +
      $"For a spot at image fraction (ix, iy), send x={minX}+{Format(max.X - min.X)}*ix and y={minY}+{Format(max.Y - min.Y)}*iy. " +
      "Simpler: capture the whole desktop with display=-1.";
  }
}
