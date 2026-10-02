namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>A parsed key combination such as ctrl+alt+delete: modifier keys plus one target key.</summary>
public sealed record KeyChord(IReadOnlyList<ModifierKey> Modifiers, Key Target)
{
  public override string ToString() =>
    Modifiers.Count == 0
      ? Target.ToString()
      : string.Join('+', Modifiers) + "+" + Target;
}
