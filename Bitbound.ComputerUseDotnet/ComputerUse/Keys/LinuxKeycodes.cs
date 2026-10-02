namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>
/// Linux evdev key codes (linux/input-event-codes.h) used by the Wayland RemoteDesktop portal
/// (NotifyKeyboardKeycode expects evdev codes) and as an X11 fallback reference.
/// </summary>
public static class LinuxKeycodes
{
  public const int KEY_BACKSPACE = 14;
  public const int KEY_ENTER = 28;
  public const int KEY_ESC = 1;
  public const int KEY_LEFTSHIFT = 42;
  public const int KEY_SPACE = 57;
  public const int KEY_TAB = 15;

  // US layout character table: character -> (evdev keycode, requires shift).
  private static readonly Dictionary<char, (int Keycode, bool Shift)> Characters = new()
  {
    ['1'] = (2, false), ['2'] = (3, false), ['3'] = (4, false), ['4'] = (5, false),
    ['5'] = (6, false), ['6'] = (7, false), ['7'] = (8, false), ['8'] = (9, false),
    ['9'] = (10, false), ['0'] = (11, false),
    ['q'] = (16, false), ['w'] = (17, false), ['e'] = (18, false), ['r'] = (19, false),
    ['t'] = (20, false), ['y'] = (21, false), ['u'] = (22, false), ['i'] = (23, false),
    ['o'] = (24, false), ['p'] = (25, false),
    ['a'] = (30, false), ['s'] = (31, false), ['d'] = (32, false), ['f'] = (33, false),
    ['g'] = (34, false), ['h'] = (35, false), ['j'] = (36, false), ['k'] = (37, false),
    ['l'] = (38, false),
    ['z'] = (44, false), ['x'] = (45, false), ['c'] = (46, false), ['v'] = (47, false),
    ['b'] = (48, false), ['n'] = (49, false), ['m'] = (50, false),
    ['-'] = (12, false), ['='] = (13, false), ['['] = (26, false), [']'] = (27, false),
    ['\\'] = (43, false), [';'] = (39, false), ['\''] = (40, false), [','] = (51, false),
    ['.'] = (52, false), ['/'] = (53, false), ['`'] = (41, false), [' '] = (57, false),
    ['Q'] = (16, true), ['W'] = (17, true), ['E'] = (18, true), ['R'] = (19, true),
    ['T'] = (20, true), ['Y'] = (21, true), ['U'] = (22, true), ['I'] = (23, true),
    ['O'] = (24, true), ['P'] = (25, true),
    ['A'] = (30, true), ['S'] = (31, true), ['D'] = (32, true), ['F'] = (33, true),
    ['G'] = (34, true), ['H'] = (35, true), ['J'] = (36, true), ['K'] = (37, true),
    ['L'] = (38, true),
    ['Z'] = (44, true), ['X'] = (45, true), ['C'] = (46, true), ['V'] = (47, true),
    ['B'] = (48, true), ['N'] = (49, true), ['M'] = (50, true),
    ['!'] = (2, true), ['@'] = (3, true), ['#'] = (4, true), ['$'] = (5, true),
    ['%'] = (6, true), ['^'] = (7, true), ['&'] = (8, true), ['*'] = (9, true),
    ['('] = (10, true), [')'] = (11, true),
    ['_'] = (12, true), ['+'] = (13, true), ['{'] = (26, true), ['}'] = (27, true),
    ['|'] = (43, true), [':'] = (39, true), ['"'] = (40, true), ['<'] = (51, true),
    ['>'] = (52, true), ['?'] = (53, true), ['~'] = (41, true),
    ['\n'] = (28, false), ['\t'] = (15, false),
  };
  private static readonly Dictionary<string, int> NamedKeys = new(StringComparer.Ordinal)
  {
    ["enter"] = 28,
    ["escape"] = 1,
    ["tab"] = 15,
    ["space"] = 57,
    ["backspace"] = 14,
    ["delete"] = 111,
    ["insert"] = 110,
    ["home"] = 102,
    ["end"] = 107,
    ["pageup"] = 104,
    ["pagedown"] = 109,
    ["up"] = 103,
    ["down"] = 108,
    ["left"] = 105,
    ["right"] = 106,
    ["capslock"] = 58,
    ["numlock"] = 69,
    ["scrolllock"] = 70,
    ["pause"] = 119,
    ["printscreen"] = 99,
    ["menu"] = 127,
    ["minus"] = 12,
    ["equals"] = 13,
    ["leftbracket"] = 26,
    ["rightbracket"] = 27,
    ["backslash"] = 43,
    ["semicolon"] = 39,
    ["quote"] = 40,
    ["comma"] = 51,
    ["period"] = 52,
    ["slash"] = 53,
    ["grave"] = 41,
    ["plus"] = 13,
  };

  /// <summary>evdev button codes (BTN_*) used by NotifyPointerButton.</summary>
  public static int GetButtonCode(MouseButton button) => button switch
  {
    MouseButton.Left => 0x110,
    MouseButton.Right => 0x111,
    MouseButton.Middle => 0x112,
    MouseButton.Extra => 0x114,
    MouseButton.Side => 0x113,
    _ => throw new ArgumentOutOfRangeException(nameof(button), button, null),
  };

  /// <summary>The evdev code for a chord modifier (left-hand variants).</summary>
  public static int GetModifierKeycode(ModifierKey modifier) => modifier switch
  {
    ModifierKey.Shift => KEY_LEFTSHIFT,
    ModifierKey.Control => 29,
    ModifierKey.Alt => 56,
    ModifierKey.Meta => 125,
    _ => throw new ArgumentOutOfRangeException(nameof(modifier), modifier, null),
  };

  /// <summary>Maps a US-layout character to an evdev key code plus shift requirement.</summary>
  public static bool TryGetCharacter(char character, out int keycode, out bool requiresShift)
  {
    if (character is '\r')
    {
      character = '\n';
    }

    if (Characters.TryGetValue(character, out var entry))
    {
      keycode = entry.Keycode;
      requiresShift = entry.Shift;
      return true;
    }

    keycode = 0;
    requiresShift = false;
    return false;
  }

  /// <summary>Maps a normalized key name to its evdev key code.</summary>
  public static bool TryGetKeycode(string normalizedName, out int keycode)
  {
    if (NamedKeys.TryGetValue(normalizedName, out keycode))
    {
      return true;
    }

    if (normalizedName.Length is >= 2 and <= 3 && normalizedName[0] == 'f' &&
        int.TryParse(normalizedName.AsSpan(1), out var number) && number is >= 1 and <= 24)
    {
      keycode = number switch
      {
        <= 10 => 58 + number,
        11 => 87,
        12 => 88,
        13 => 183,
        14 => 184,
        15 => 185,
        16 => 186,
        17 => 187,
        18 => 188,
        19 => 189,
        20 => 190,
        21 => 191,
        22 => 192,
        23 => 193,
        _ => 194,
      };

      return true;
    }

    return false;
  }
}
