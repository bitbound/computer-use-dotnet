namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>
/// A single key target: either a printable character (e.g. 'a', 'K', '@')
/// or a named key (e.g. "enter", "f5", "delete"). Names are normalized to lowercase.
/// </summary>
public sealed record Key
{
  private Key(string? name, char? character)
  {
    Name = name;
    Character = character;
  }

  /// <summary>Normalized lowercase key name, when this key was given by name.</summary>
  public string? Name { get; }

  /// <summary>Printable character, when this key was given as a single character.</summary>
  public char? Character { get; }

  public static Key FromName(string normalizedName) => new(normalizedName, null);

  public static Key FromCharacter(char character) => new(null, character);

  public override string ToString() => Character?.ToString() ?? Name ?? "?";
}
