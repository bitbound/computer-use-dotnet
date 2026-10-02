namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>
/// Parses key chord strings like "ctrl+alt+delete", "cmd+space", or "a" into a
/// <see cref="KeyChord"/>. Tokens are case-insensitive and separated by '+'.
/// </summary>
public static class KeyChordParser
{

  /// <summary>Canonical names for non-character keys accepted by <see cref="Parse"/>.</summary>
  public static readonly IReadOnlyCollection<string> KnownKeyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
  {
    "enter", "return", "escape", "esc", "tab", "space", "backspace", "delete", "insert",
    "home", "end", "pageup", "pagedown", "up", "down", "left", "right",
    "capslock", "numlock", "scrolllock", "pause", "printscreen", "menu",
    "plus", "minus", "equals", "comma", "period", "slash", "backslash",
    "semicolon", "quote", "grave", "leftbracket", "rightbracket",
    "f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "f9", "f10", "f11", "f12",
    "f13", "f14", "f15", "f16", "f17", "f18", "f19", "f20", "f21", "f22", "f23", "f24",
  };

  private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
  {
    ["return"] = "enter",
    ["esc"] = "escape",
    ["spacebar"] = "space",
    ["arrowup"] = "up",
    ["arrowdown"] = "down",
    ["arrowleft"] = "left",
    ["arrowright"] = "right",
    ["pgup"] = "pageup",
    ["pgdn"] = "pagedown",
    ["pgdown"] = "pagedown",
    ["del"] = "delete",
    ["ins"] = "insert",
    ["prntscr"] = "printscreen",
    ["prtsc"] = "printscreen",
    ["contextmenu"] = "menu",
    ["equal"] = "equals",
    ["dash"] = "minus",
    ["hyphen"] = "minus",
    ["backtick"] = "grave",
    ["bracketleft"] = "leftbracket",
    ["bracketright"] = "rightbracket",
    ["openbracket"] = "leftbracket",
    ["closebracket"] = "rightbracket",
  };
  private static readonly Dictionary<string, ModifierKey> Modifiers = new(StringComparer.OrdinalIgnoreCase)
  {
    ["shift"] = ModifierKey.Shift,
    ["shiftkey"] = ModifierKey.Shift,
    ["control"] = ModifierKey.Control,
    ["ctrl"] = ModifierKey.Control,
    ["ctl"] = ModifierKey.Control,
    ["alt"] = ModifierKey.Alt,
    ["option"] = ModifierKey.Alt,
    ["opt"] = ModifierKey.Alt,
    ["meta"] = ModifierKey.Meta,
    ["cmd"] = ModifierKey.Meta,
    ["command"] = ModifierKey.Meta,
    ["super"] = ModifierKey.Meta,
    ["win"] = ModifierKey.Meta,
    ["windows"] = ModifierKey.Meta,
  };

  /// <summary>
  /// Parses a chord such as "ctrl+shift+t". Exactly one target key (named or a single
  /// printable character) is required; zero or multiple targets throw <see cref="FormatException"/>.
  /// </summary>
  public static KeyChord Parse(string chord)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(chord);

    var tokens = chord
      .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    if (tokens.Length == 0)
    {
      throw new FormatException($"Key chord '{chord}' is empty.");
    }

    var modifiers = new List<ModifierKey>();
    var targets = new List<Key>();

    foreach (var token in tokens)
    {
      if (Modifiers.TryGetValue(token, out var modifier))
      {
        if (!modifiers.Contains(modifier))
        {
          modifiers.Add(modifier);
        }

        continue;
      }

      targets.Add(ParseTarget(token));
    }

    // A literal '+' typed as the final token is the plus key (tokens split on '+', so a
    // trailing '+' disappears; treat a chord ending in '+' as intending the '+' key).
    if (targets.Count == 0 && chord.EndsWith("+", StringComparison.Ordinal))
    {
      targets.Add(Key.FromCharacter('+'));
    }

    if (targets.Count == 0)
    {
      throw new FormatException($"Key chord '{chord}' has no target key (only modifiers).");
    }

    if (targets.Count > 1)
    {
      throw new FormatException($"Key chord '{chord}' has more than one target key; modifiers only may precede a single target.");
    }

    return new KeyChord(modifiers, targets[0]);
  }

  /// <summary>Normalizes a key token to a <see cref="Key"/> (alias resolution + character detection).</summary>
  public static Key ParseTarget(string token)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(token);

    var trimmed = token.Trim();

    if (trimmed.Length == 1 && !char.IsControl(trimmed[0]) && trimmed[0] != ' ')
    {
      return Key.FromCharacter(trimmed[0]);
    }

    var lower = trimmed.ToLowerInvariant();

    var name = Aliases.TryGetValue(lower, out var alias) ? alias : lower;

    // There is no unshifted "+" key; the name always denotes the '+' character,
    // which every backend types via its shifted-character mapping.
    if (name == "plus")
    {
      return Key.FromCharacter('+');
    }

    if (!KnownKeyNames.Contains(name))
    {
      throw new FormatException(
        $"Unknown key '{token}'. Use a single character, a known key name (e.g. enter, tab, escape, up, f5), or a '+'-separated chord like ctrl+alt+delete.");
    }

    return Key.FromName(name);
  }
}
