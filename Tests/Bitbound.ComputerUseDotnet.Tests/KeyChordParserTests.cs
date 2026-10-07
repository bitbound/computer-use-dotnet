using Bitbound.ComputerUseDotnet.ComputerUse;

namespace Bitbound.ComputerUseDotnet.Tests;

/// <summary>Tests for <see cref="KeyChordParser"/>.</summary>
public class KeyChordParserTests
{
  [Fact]
  public void Parse_SingleCharacter_ReturnsCharacterTarget()
  {
    var chord = KeyChordParser.Parse("a");

    Assert.Empty(chord.Modifiers);
    Assert.Equal('a', chord.Target.Character);
    Assert.Null(chord.Target.Name);
  }

  [Fact]
  public void Parse_ModifierChord_ReturnsModifiersAndTarget()
  {
    var chord = KeyChordParser.Parse("ctrl+alt+delete");

    Assert.Equal([ModifierKey.Control, ModifierKey.Alt], chord.Modifiers);
    Assert.Equal("delete", chord.Target.Name);
  }

  [Fact]
  public void Parse_IsCaseInsensitive()
  {
    var chord = KeyChordParser.Parse("CTRL+Shift+T");

    Assert.Equal([ModifierKey.Control, ModifierKey.Shift], chord.Modifiers);

    // Character targets keep their case: uppercase carries shift semantics for the backends.
    Assert.Equal('T', chord.Target.Character);
  }

  [Fact]
  public void Parse_AliasNames_ResolveToCanonicalName()
  {
    var chord = KeyChordParser.Parse("cmd+pgup");

    Assert.Equal([ModifierKey.Meta], chord.Modifiers);
    Assert.Equal("pageup", chord.Target.Name);
  }

  [Fact]
  public void Parse_SpaceCharacter_IsNamedSpace()
  {
    var chord = KeyChordParser.Parse("ctrl+space");

    Assert.Equal("space", chord.Target.Name);
  }

  [Fact]
  public void Parse_DuplicateModifier_IsCollapsed()
  {
    var chord = KeyChordParser.Parse("ctrl+ctrl+c");

    Assert.Single(chord.Modifiers);
  }

  [Fact]
  public void Parse_TrailingPlus_TypesPlusKey()
  {
    var chord = KeyChordParser.Parse("ctrl++");

    Assert.Equal('+', chord.Target.Character);
  }

  [Fact]
  public void Parse_PlusName_ResolvesToPlusCharacter()
  {
    var target = KeyChordParser.ParseTarget("plus");

    Assert.Equal('+', target.Character);
    Assert.Null(target.Name);
  }

  [Fact]
  public void Parse_OnlyModifiers_Throws()
  {
    _ = Assert.Throws<FormatException>(() => KeyChordParser.Parse("ctrl+shift"));
  }

  [Fact]
  public void Parse_TwoTargets_Throws()
  {
    _ = Assert.Throws<FormatException>(() => KeyChordParser.Parse("a+b"));
  }

  [Fact]
  public void Parse_UnknownName_Throws()
  {
    _ = Assert.Throws<FormatException>(() => KeyChordParser.Parse("ctrl+notaprothing"));
  }

  [Fact]
  public void Parse_Empty_Throws()
  {
    _ = Assert.Throws<ArgumentException>(() => KeyChordParser.Parse(" "));
  }
}
