using Bitbound.ComputerUseDotnet.ComputerUse.Windows;

namespace Bitbound.ComputerUseDotnet.Tests;

/// <summary>Tests for the platform-independent UIA element cache, action table, and formatting.</summary>
public sealed class WindowsUiaServiceTests
{
  [Fact]
  public void Add_WhenCalled_ReturnsIncrementingIds()
  {
    var cache = new UiaElementCache();

    var first = cache.Add(1, new object(), Snapshot());
    var second = cache.Add(2, new object(), Snapshot());

    Assert.Equal("e1", first);
    Assert.Equal("e2", second);
    Assert.Equal(2, cache.Count);
  }

  [Fact]
  public void Add_WhenSnapshotHasNoId_StampsTheGeneratedId()
  {
    var cache = new UiaElementCache();

    var id = cache.Add(1, new object(), Snapshot());

    Assert.True(cache.TryGet(id, out var cached));
    Assert.Equal(id, cached.Snapshot.Id);
  }

  [Fact]
  public void TryGet_WithUnknownId_ReturnsFalse()
  {
    var cache = new UiaElementCache();

    Assert.False(cache.TryGet("e99", out _));
  }

  [Fact]
  public void RemoveWindow_WhenTwoWindowsCached_DropsOnlyThatWindow()
  {
    var cache = new UiaElementCache();
    var kept = cache.Add(10, new object(), Snapshot());
    var dropped = cache.Add(20, new object(), Snapshot());

    var removed = cache.RemoveWindow(20);

    Assert.Equal(1, removed);
    Assert.True(cache.TryGet(kept, out _));
    Assert.False(cache.TryGet(dropped, out _));
  }

  [Fact]
  public void Add_AfterRemovingWindow_ContinuesNumberingSoOldIdsNeverResolve()
  {
    var cache = new UiaElementCache();
    var oldId = cache.Add(10, new object(), Snapshot());
    cache.RemoveWindow(10);

    var newId = cache.Add(10, new object(), Snapshot());

    Assert.NotEqual(oldId, newId);
    Assert.False(cache.TryGet(oldId, out _));
  }

  [Theory]
  [InlineData("invoke", "Invoke")]
  [InlineData("TOGGLE", "Toggle")]
  [InlineData(" expand ", "Expand")]
  [InlineData("collapse", "Collapse")]
  [InlineData("set_value", "SetValue")]
  [InlineData("select", "Select")]
  [InlineData("scroll_into_view", "ScrollIntoView")]
  public void TryParse_WithKnownAction_Parses(string action, string expected)
  {
    Assert.True(UiaActions.TryParse(action, out var parsed));
    Assert.Equal(expected, parsed.ToString());
  }

  [Theory]
  [InlineData("")]
  [InlineData("click")]
  [InlineData("double_click")]
  [InlineData("expand_collapse")]
  public void TryParse_WithUnknownAction_ReturnsFalse(string action)
  {
    Assert.False(UiaActions.TryParse(action, out _));
  }

  [Fact]
  public void TryParse_WithPaddedInvoke_Parses()
  {
    Assert.True(UiaActions.TryParse("  invoke  ", out var parsed));
    Assert.Equal(UiaAction.Invoke, parsed);
  }

  [Fact]
  public void ValidateValue_WhenSetValueHasNoValue_Throws()
  {
    Assert.Throws<InvalidOperationException>(() => UiaActions.ValidateValue(UiaAction.SetValue, null));
    Assert.Throws<InvalidOperationException>(() => UiaActions.ValidateValue(UiaAction.SetValue, string.Empty));
  }

  [Fact]
  public void ValidateValue_WhenSetValueHasValue_DoesNotThrow()
  {
    UiaActions.ValidateValue(UiaAction.SetValue, "hello");
  }

  [Fact]
  public void ValidateValue_WhenNonSetValueActionHasValue_Throws()
  {
    Assert.Throws<InvalidOperationException>(() => UiaActions.ValidateValue(UiaAction.Invoke, "hello"));
  }

  [Theory]
  [InlineData("invoke", "invoke")]
  [InlineData("toggle", "toggle")]
  [InlineData("expand", "expand_collapse")]
  [InlineData("collapse", "expand_collapse")]
  [InlineData("set_value", "value")]
  [InlineData("select", "selection_item")]
  [InlineData("scroll_into_view", "scroll_item")]
  public void RequiredPattern_ReturnsExpectedPattern(string action, string expected)
  {
    Assert.True(UiaActions.TryParse(action, out var parsed));
    Assert.Equal(expected, UiaActions.RequiredPattern(parsed));
  }

  [Fact]
  public void ElementLine_FormatsIdTypeRectAndPatterns()
  {
    var line = UiaFormat.ElementLine(Snapshot(id: "e3", name: "OK", automationId: "OkButton", patterns: ["invoke", "scroll_item"]));

    Assert.Equal("[e3] Button \"OK\" automationId=OkButton rect=(10, 20) 80x24 patterns=invoke,scroll_item", line);
  }

  [Fact]
  public void ElementLine_WhenAutomationIdEmpty_OmitsIt()
  {
    var line = UiaFormat.ElementLine(Snapshot(name: "Send", automationId: string.Empty, patterns: []));

    Assert.Equal("[e1] Button \"Send\" rect=(10, 20) 80x24 patterns=none", line);
  }

  [Fact]
  public void ListHeader_WhenTruncated_MentionsTheElementLimit()
  {
    var header = UiaFormat.ListHeader("Window 0x1 \"Note\"", 200, truncated: true, depthLimitReached: 3);

    Assert.Contains("200 element(s) listed", header);
    Assert.Contains("element limit", header);
    Assert.Contains("max_depth", header);
  }

  [Fact]
  public void State_WithOnlyToggle_OmitsTheOtherSections()
  {
    var text = UiaFormat.State(Snapshot(id: "e2"), enabled: "yes", offscreen: null, value: null, toggle: "On", rangeValue: null, expandCollapse: null, selected: null);

    Assert.Contains("Toggle: On", text);
    Assert.Contains("Enabled: yes", text);
    Assert.DoesNotContain("Value:", text);
    Assert.DoesNotContain("RangeValue:", text);
    Assert.DoesNotContain("Selected:", text);
  }

  private static UiaElementSnapshot Snapshot(
      string id = "e1",
      string controlType = "Button",
      string name = "OK",
      string automationId = "OkButton",
      IReadOnlyList<string>? patterns = null) =>
    new(id, controlType, name, automationId, 10, 20, 80, 24, patterns ?? ["invoke"]);
}
