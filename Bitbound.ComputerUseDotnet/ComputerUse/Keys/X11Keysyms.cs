namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>
/// X11 keysym name resolution: converts normalized key names to the key *names* that
/// XStringToKeysym accepts (X11 keysym string names such as "Return" or "Page_Up").
/// </summary>
public static class X11Keysyms
{
  private static readonly Dictionary<string, string> NamedKeys = new(StringComparer.Ordinal)
  {
    ["enter"] = "Return",
    ["escape"] = "Escape",
    ["tab"] = "Tab",
    ["space"] = "space",
    ["backspace"] = "BackSpace",
    ["delete"] = "Delete",
    ["insert"] = "Insert",
    ["home"] = "Home",
    ["end"] = "End",
    ["pageup"] = "Page_Up",
    ["pagedown"] = "Page_Down",
    ["up"] = "Up",
    ["down"] = "Down",
    ["left"] = "Left",
    ["right"] = "Right",
    ["capslock"] = "Caps_Lock",
    ["numlock"] = "Num_Lock",
    ["scrolllock"] = "Scroll_Lock",
    ["pause"] = "Pause",
    ["printscreen"] = "Print",
    ["menu"] = "Menu",
    ["minus"] = "minus",
    ["equals"] = "equal",
    ["leftbracket"] = "bracketleft",
    ["rightbracket"] = "bracketright",
    ["backslash"] = "backslash",
    ["semicolon"] = "semicolon",
    ["quote"] = "apostrophe",
    ["comma"] = "comma",
    ["period"] = "period",
    ["slash"] = "slash",
    ["grave"] = "grave",
    ["plus"] = "plus",
    ["f1"] = "F1",
    ["f2"] = "F2",
    ["f3"] = "F3",
    ["f4"] = "F4",
    ["f5"] = "F5",
    ["f6"] = "F6",
    ["f7"] = "F7",
    ["f8"] = "F8",
    ["f9"] = "F9",
    ["f10"] = "F10",
    ["f11"] = "F11",
    ["f12"] = "F12",
    ["f13"] = "F13",
    ["f14"] = "F14",
    ["f15"] = "F15",
    ["f16"] = "F16",
    ["f17"] = "F17",
    ["f18"] = "F18",
    ["f19"] = "F19",
    ["f20"] = "F20",
    ["f21"] = "F21",
    ["f22"] = "F22",
    ["f23"] = "F23",
    ["f24"] = "F24",
  };

  private static readonly Dictionary<ModifierKey, string> ModifierKeysyms = new()
  {
    [ModifierKey.Shift] = "Shift_L",
    [ModifierKey.Control] = "Control_L",
    [ModifierKey.Alt] = "Alt_L",
    [ModifierKey.Meta] = "Super_L",
  };

  /// <summary>Maps a normalized key name to its X11 keysym name.</summary>
  public static bool TryGetKeysymName(string normalizedName, out string keysymName) =>
    NamedKeys.TryGetValue(normalizedName, out keysymName!);

  /// <summary>The keysym name for a chord modifier.</summary>
  public static string GetModifierKeysymName(ModifierKey modifier) => ModifierKeysyms[modifier];

  /// <summary>
  /// Maps a character to the keysym name XStringToKeysym understands.
  /// Returns the character itself when a single printable character is a valid keysym name.
  /// </summary>
  public static bool TryGetCharacterKeysymName(char character, out string keysymName)
  {
    keysymName = character switch
    {
      '\n' or '\r' => "Return",
      '\t' => "Tab",
      >= 'a' and <= 'z' => character.ToString(),
      >= 'A' and <= 'Z' => character.ToString(),
      >= '0' and <= '9' => character.ToString(),
      '!' => "exclam",
      '"' => "quotedbl",
      '#' => "numbersign",
      '$' => "dollar",
      '%' => "percent",
      '&' => "ampersand",
      '\'' => "apostrophe",
      '(' => "parenleft",
      ')' => "parenright",
      '*' => "asterisk",
      '+' => "plus",
      ',' => "comma",
      '-' => "minus",
      '.' => "period",
      '/' => "slash",
      ':' => "colon",
      ';' => "semicolon",
      '<' => "less",
      '=' => "equal",
      '>' => "greater",
      '?' => "question",
      '@' => "at",
      '[' => "bracketleft",
      '\\' => "backslash",
      ']' => "bracketright",
      '^' => "asciicircum",
      '_' => "underscore",
      '`' => "grave",
      '{' => "braceleft",
      '|' => "bar",
      '}' => "braceright",
      '~' => "asciitilde",
      ' ' => "space",
      _ => string.Empty,
    };

    return keysymName.Length > 0;
  }

  /// <summary>Whether the character requires holding Shift to produce on a US keyboard.</summary>
  public static bool CharacterRequiresShift(char character) =>
    char.IsUpper(character) || "!@#$%^&*()_+{}|:\"<>?~".Contains(character);
}
