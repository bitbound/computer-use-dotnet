namespace Bitbound.ComputerUseDotnet.ComputerUse;

public sealed partial class ComputerUseTools
{
  [McpServerTool(Name = "take_screenshot")]
  [Description(
    "Captures the screen and returns it as a PNG image. Coordinates used by the input tools are the " +
    "pixel coordinates of this image when it covers the whole virtual screen. " +
    "Returns a text block with capture metadata (size, scale, permission hints) followed by the image.")]
  public async Task<IEnumerable<ContentBlock>> TakeScreenshot(
      [Description("Display index to capture (see get_desktop_info). -1 captures the entire virtual screen (all displays). Default: -1.")]
        int display = -1,
      [Description("Optionally downscale the returned image so its longest side is at most this many pixels (token saving). 0 keeps native resolution. Input coordinates still refer to the native-size space.")]
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

    string? note = null;

    if (scaled)
    {
      var factor = (double)sourceWidth / working.Width;
      note = $"Image was downscaled {factor:0.###}x; convert image pixels to input coordinates by multiplying by {factor:0.###}.";
    }

    var text =
      $"Capture: {_backend.BackendName}. " +
      $"Image {working.Width}x{working.Height}px (virtual screen {layout.Width}x{layout.Height}px" +
      (display >= 0 ? $", display {display} crop {sourceWidth}x{sourceHeight}px" : ", full virtual screen") + "). " +
      (note ?? "Input coordinates map 1:1 to image pixels.") +
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
}
