using Bitbound.ComputerUseDotnet.ComputerUse.Windows;

namespace Bitbound.ComputerUseDotnet.Tests;

/// <summary>Tests for the platform-independent window resolution logic.</summary>
public sealed class WindowsWindowServiceTests
{
  [Theory]
  [InlineData("0x1A2B", 0x1A2B)]
  [InlineData("0X1a2b", 0x1A2B)]
  [InlineData(" 0x10 ", 0x10)]
  [InlineData("12345", 12345)]
  public void TryParseHandle_WithHandleText_ParsesHandle(string text, long expected)
  {
    Assert.True(WindowsWindowService.TryParseHandle(text, out var handle));
    Assert.Equal(expected, (long)handle);
  }

  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("notepad")]
  [InlineData("0xzz")]
  [InlineData("12.5")]
  public void TryParseHandle_WithNonHandleText_ReturnsFalse(string text)
  {
    Assert.False(WindowsWindowService.TryParseHandle(text, out var handle));
    Assert.Equal(nint.Zero, handle);
  }

  [Fact]
  public void FormatHandle_FormatsAsHex()
  {
    Assert.Equal("0x1A2B", WindowsWindowService.FormatHandle(0x1A2B));
  }

  [Fact]
  public void SelectBestMatch_WhenExactTitleExists_PrefersItOverLargerWindow()
  {
    var exact = Window(title: "Notepad", area: 10);
    var partial = Window(title: "Untitled - Notepad", area: 10_000);

    var best = WindowsWindowService.SelectBestMatch([partial, exact], foregroundHandle: 999, query: "Notepad");

    Assert.Equal(exact.Handle, best.Handle);
  }

  [Fact]
  public void SelectBestMatch_WhenNoExactTitle_PrefersForeground()
  {
    var foreground = Window(handle: 42, title: "Note - Notepad", area: 10);
    var other = Window(handle: 43, title: "Notepad Backup", area: 10_000);

    var best = WindowsWindowService.SelectBestMatch([other, foreground], foregroundHandle: 42, query: "notepad");

    Assert.Equal(42, (long)best.Handle);
  }

  [Fact]
  public void SelectBestMatch_WhenNoExactOrForeground_PrefersVisible()
  {
    var hidden = Window(title: "Notepad (hidden)", area: 10_000, visible: false);
    var visible = Window(title: "Notepad (visible)", area: 10);

    var best = WindowsWindowService.SelectBestMatch([hidden, visible], foregroundHandle: 0, query: "notepad");

    Assert.Equal(visible.Handle, best.Handle);
  }

  [Fact]
  public void SelectBestMatch_WhenStateIsEqual_PrefersLargerArea()
  {
    var small = Window(title: "Notepad small", area: 10);
    var large = Window(title: "Notepad large", area: 10_000);

    var best = WindowsWindowService.SelectBestMatch([small, large], foregroundHandle: 0, query: "notepad");

    Assert.Equal(large.Handle, best.Handle);
  }

  [Fact]
  public async Task FindByTitle_WithBlankQuery_Throws()
  {
    var service = new WindowsWindowService();

    await Assert.ThrowsAsync<ArgumentException>(() => Task.Run(() => service.FindByTitle("  ")));
  }

  private static WindowInfo Window(string title, long area, nint? handle = null, bool visible = true)
  {
    var side = (int)Math.Max(1, Math.Round(Math.Sqrt(area)));

    return new WindowInfo
    {
      Handle = handle ?? Guid.NewGuid().GetHashCode() & int.MaxValue,
      Title = title,
      ClassName = "TestWindow",
      ProcessId = 1,
      X = 0,
      Y = 0,
      Width = side,
      Height = (int)(area / side),
      IsVisible = visible,
    };
  }
}
