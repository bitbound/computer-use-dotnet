using System.Text;
using Bitbound.ComputerUseDotnet.ComputerUse.Windows;
using Microsoft.Extensions.Logging;

namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>
/// Windows-only MCP tools for enumerating top-level windows and capturing a single
/// window with PrintWindow. Registered only when the server runs on Windows.
/// </summary>
[McpServerToolType]
public sealed class WindowsWindowTools(WindowsWindowService windows, IComputerUseBackend backend, ILogger<WindowsWindowTools> logger)
{
  private readonly IComputerUseBackend _backend = backend;
  private readonly ILogger<WindowsWindowTools> _logger = logger;
  private readonly WindowsWindowService _windows = windows;

  [McpServerTool(Name = "capture_window")]
  [Description(
    "Captures one desktop window (Windows only) with PrintWindow and returns it as a PNG image plus " +
    "a text block of capture metadata. PrintWindow renders from the window's own device context, so it " +
    "works even when the window is covered by others, without touching the screen. Hardware-accelerated " +
    "content (some games, GPU-composited pages) may render blank; use take_screenshot if that happens.")]
  public Task<IEnumerable<ContentBlock>> CaptureWindow(
      [Description("Window handle (0x-prefixed hex or decimal) or a title substring.")]
        string window,
      [Description("Optionally downscale the returned image so its longest side is at most this many pixels (token saving). 0 keeps native resolution.")]
        int max_side = 0) => RunToolAsync<IEnumerable<ContentBlock>>("capture_window", async () =>
  {
    var (info, bitmap) = await Task.Run(() =>
    {
      var resolved = _windows.Resolve(window);
      return (resolved, _windows.CaptureWindow(resolved.Handle));
    });

    using (bitmap)
    {
      var resize = ComputerUseTools.DownscaleToMaxSide(bitmap, max_side);
      var working = resize ?? bitmap;

      try
      {
        using var image = SKImage.FromBitmap(working);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);

        var text =
          $"Capture: PrintWindow. Window {WindowsWindowService.FormatHandle(info.Handle)} \"{info.Title}\" " +
          $"({info.ProcessName ?? "unknown process"}), bounds ({info.X}, {info.Y}) {info.Width}x{info.Height} physical px. " +
          $"Image {working.Width}x{working.Height}px." +
          (resize is not null ? " Downscaled to save tokens. " : " ") +
          "The image is just this window, not the desktop. To click inside it, take_screenshot the whole " +
          "desktop or use the desktop fractions from get_window_info.";

        return new List<ContentBlock>
        {
          new TextContentBlock { Text = text },
          ImageContentBlock.FromBytes(data.ToArray(), "image/png"),
        };
      }
      finally
      {
        resize?.Dispose();
      }
    }
  });

  [McpServerTool(Name = "get_window_info")]
  [Description(
    "Describes one desktop window (Windows only): handle, title, class, process, bounds in " +
    "physical screen pixels, and state (visible, minimized, foreground, cloaked, tool window, owned). " +
    "Pass a handle from list_windows (e.g. 0x1A2B3C) or a title substring.")]
  public Task<string> GetWindowInfo(
      [Description("Window handle (0x-prefixed hex or decimal) or a title substring.")]
        string window) => RunToolAsync("get_window_info", async () =>
  {
    var info = await Task.Run(() => _windows.Resolve(window));
    var fraction = await DesktopFraction(info);

    var builder = new StringBuilder();
    builder.AppendLine(DescribeLine(info));
    builder.AppendLine($"Class: {info.ClassName}");
    builder.AppendLine($"Process: {info.ProcessName ?? "unknown"} (pid {info.ProcessId})");
    builder.AppendLine($"Flags: visible={info.IsVisible} minimized={info.IsMinimized} foreground={info.IsForeground} " +
      $"cloaked={info.IsCloaked} tool={info.IsToolWindow} owned={info.HasOwner}");

    if (fraction.Min is { } min && fraction.Max is { } max)
    {
      builder.AppendLine(
        $"Desktop fractions for input tools: x {ComputerUseTools.Format(min.X)} to {ComputerUseTools.Format(max.X)}, " +
        $"y {ComputerUseTools.Format(min.Y)} to {ComputerUseTools.Format(max.Y)}. " +
        "Capture the whole desktop with take_screenshot to click inside this window.");
    }

    return builder.ToString().TrimEnd();
  });

  [McpServerTool(Name = "list_windows")]
  [Description(
    "Lists top-level desktop windows (Windows only). By default returns alt-tab-eligible windows: " +
    "visible, uncloaked, titled, not tool windows, and unowned. Each line gives the window handle " +
    "(e.g. 0x1A2B3C), title, class, process, bounds in physical screen pixels, and state flags. " +
    "Use the handle with get_window_info or capture_window.")]
  public Task<string> ListWindows(
      [Description("Include hidden, cloaked, and zero-area windows too. Default: false.")]
        bool include_invisible = false,
      [Description("Include WS_EX_TOOLWINDOW and owned windows, which alt-tab hides. Default: false.")]
        bool include_tool_windows = false,
      [Description("Optional case-insensitive substring filter on the window title.")]
        string? title = null,
      [Description("Optional case-insensitive substring filter on the owning process name.")]
        string? process = null) => RunToolAsync("list_windows", async () =>
  {
    var result = await Task.Run(() => _windows.EnumerateWindows(include_invisible, include_tool_windows));

    if (title is not null)
    {
      result = result.Where(w => w.Title.Contains(title, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    if (process is not null)
    {
      result = result.Where(w => w.ProcessName?.Contains(process, StringComparison.OrdinalIgnoreCase) == true).ToArray();
    }

    if (result.Count == 0)
    {
      return "No windows matched. Widen the filters (include_invisible, include_tool_windows) or drop title/process.";
    }

    var builder = new StringBuilder();
    builder.AppendLine($"Windows: {result.Count} (bounds are physical screen pixels; use get_desktop_info for the desktop size).");

    foreach (var window in result)
    {
      builder.AppendLine(DescribeLine(window));
    }

    return builder.ToString().TrimEnd();
  });

  private static string DescribeLine(WindowInfo window) =>
    $"[{WindowsWindowService.FormatHandle(window.Handle)}] \"{window.Title}\" " +
    $"pid={window.ProcessId} process={window.ProcessName ?? "?"} " +
    $"bounds=({window.X}, {window.Y}) {window.Width}x{window.Height}" +
    (window.IsForeground ? " [foreground]" : string.Empty) +
    (window.IsMinimized ? " [minimized]" : string.Empty) +
    (!window.IsVisible ? " [hidden]" : string.Empty) +
    (window.IsCloaked ? " [cloaked]" : string.Empty) +
    (window.IsToolWindow ? " [tool]" : string.Empty) +
    (window.HasOwner ? " [owned]" : string.Empty);

  /// <summary>
  /// Window bounds as desktop fractions, or nulls when the desktop layout cannot be read.
  /// Bounds are physical pixels while fractions use the logical layout; they diverge only
  /// under mixed-DPI scaling, which the fraction text warns about implicitly by pointing at take_screenshot.
  /// </summary>
  private async Task<(NormalizedPoint? Min, NormalizedPoint? Max)> DesktopFraction(WindowInfo window)
  {
    try
    {
      var layout = await _backend.GetDisplayLayoutAsync();
      return layout.FractionBoundsOf(new DisplayInfo
      {
        Index = 0,
        Name = "window",
        X = window.X,
        Y = window.Y,
        Width = window.Width,
        Height = window.Height,
      });
    }
    catch (Exception)
    {
      return (null, null);
    }
  }

  private Task<T> RunToolAsync<T>(string toolName, Func<Task<T>> action) =>
    ComputerUseTools.RunToolAsync(toolName, _logger, action);
}
