using Bitbound.ComputerUseDotnet.ComputerUse;

namespace Bitbound.ComputerUseDotnet.Tests;

/// <summary>Spot checks for the per-platform key tables used by the backends.</summary>
public class KeyTablesTests
{
  [Theory]
  [InlineData("enter", true)]
  [InlineData("pageup", true)]
  [InlineData("f12", true)]
  [InlineData("notakey", false)]
  public void WindowsVirtualKeys_ResolvesCanonicalNames(string name, bool expected)
  {
    var found = WindowsVirtualKeys.TryGetVirtualKey(name, out _);
    Assert.Equal(expected, found);
  }

  [Fact]
  public void WindowsVirtualKeys_ExtendedKeys_AreFlagged()
  {
    Assert.True(WindowsVirtualKeys.IsExtended("delete"));
    Assert.False(WindowsVirtualKeys.IsExtended("enter"));
  }

  [Fact]
  public void WindowsVirtualKeys_Modifiers_UseLeftVariants()
  {
    Assert.Equal((ushort)0xA2, WindowsVirtualKeys.GetModifierVirtualKey(ModifierKey.Control));
    Assert.Equal((ushort)0xA0, WindowsVirtualKeys.GetModifierVirtualKey(ModifierKey.Shift));
  }

  [Theory]
  [InlineData("enter", (ushort)36)]
  [InlineData("tab", (ushort)48)]
  [InlineData("escape", (ushort)53)]
  public void MacVirtualKeys_KnownCodes(string name, ushort expected)
  {
    Assert.True(MacVirtualKeys.TryGetVirtualKey(name, out var code));
    Assert.Equal(expected, code);
  }

  [Fact]
  public void MacVirtualKeys_UppercaseCharacter_RequiresShift()
  {
    Assert.True(MacVirtualKeys.TryGetCharacter('A', out var code, out var shifted));
    Assert.Equal((ushort)0, code);
    Assert.True(shifted);
  }

  [Fact]
  public void LinuxKeycodes_CharacterTable_MatchesEvdevUsLayout()
  {
    Assert.True(LinuxKeycodes.TryGetCharacter('a', out var code, out var shifted));
    Assert.Equal(30, code);
    Assert.False(shifted);

    Assert.True(LinuxKeycodes.TryGetCharacter('!', out code, out shifted));
    Assert.Equal(2, code);
    Assert.True(shifted);
  }

  [Theory]
  [InlineData("f1", 59)]
  [InlineData("f11", 87)]
  [InlineData("pagedown", 109)]
  public void LinuxKeycodes_NamedKeys(string name, int expected)
  {
    Assert.True(LinuxKeycodes.TryGetKeycode(name, out var code));
    Assert.Equal(expected, code);
  }

  [Fact]
  public void LinuxKeycodes_ModifiersAndButtons_UseEvdevCodes()
  {
    Assert.Equal(29, LinuxKeycodes.GetModifierKeycode(ModifierKey.Control));
    Assert.Equal(0x110, LinuxKeycodes.GetButtonCode(MouseButton.Left));
  }

  [Theory]
  [InlineData("pageup", "Page_Up")]
  [InlineData("enter", "Return")]
  public void X11Keysyms_CanonicalNames(string name, string expected)
  {
    Assert.True(X11Keysyms.TryGetKeysymName(name, out var keysym));
    Assert.Equal(expected, keysym);
  }

  [Fact]
  public void X11Keysyms_Characters_MapToKeysymNames()
  {
    Assert.True(X11Keysyms.TryGetCharacterKeysymName('A', out var keysym));
    Assert.Equal("A", keysym);
    Assert.True(X11Keysyms.CharacterRequiresShift('A'));

    Assert.True(X11Keysyms.TryGetCharacterKeysymName('~', out keysym));
    Assert.Equal("asciitilde", keysym);
    Assert.True(X11Keysyms.CharacterRequiresShift('~'));

    Assert.False(X11Keysyms.TryGetCharacterKeysymName('€', out _));
  }
}
