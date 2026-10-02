namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>
/// macOS virtual key codes (kVK_*) for named keys, characters (US layout), and chord modifiers,
/// plus CoreGraphics modifier flag masks.
/// </summary>
public static class MacVirtualKeys
{
  public const ushort kVK_ANSI_A = 0x00;
  public const ushort kVK_ANSI_S = 0x01;
  public const ushort kVK_ANSI_D = 0x02;
  public const ushort kVK_ANSI_F = 0x03;
  public const ushort kVK_ANSI_H = 0x04;
  public const ushort kVK_ANSI_G = 0x05;
  public const ushort kVK_ANSI_Z = 0x06;
  public const ushort kVK_ANSI_X = 0x07;
  public const ushort kVK_ANSI_C = 0x08;
  public const ushort kVK_ANSI_V = 0x09;
  public const ushort kVK_ANSI_B = 0x0B;
  public const ushort kVK_ANSI_Q = 0x0C;
  public const ushort kVK_ANSI_W = 0x0D;
  public const ushort kVK_ANSI_E = 0x0E;
  public const ushort kVK_ANSI_R = 0x0F;
  public const ushort kVK_ANSI_Y = 0x10;
  public const ushort kVK_ANSI_T = 0x11;
  public const ushort kVK_ANSI_1 = 0x12;
  public const ushort kVK_ANSI_2 = 0x13;
  public const ushort kVK_ANSI_3 = 0x14;
  public const ushort kVK_ANSI_4 = 0x15;
  public const ushort kVK_ANSI_6 = 0x16;
  public const ushort kVK_ANSI_5 = 0x17;
  public const ushort kVK_ANSI_Equal = 0x18;
  public const ushort kVK_ANSI_9 = 0x19;
  public const ushort kVK_ANSI_7 = 0x1A;
  public const ushort kVK_ANSI_8 = 0x1C;
  public const ushort kVK_ANSI_0 = 0x1D;
  public const ushort kVK_ANSI_RightBracket = 0x1E;
  public const ushort kVK_ANSI_O = 0x1F;
  public const ushort kVK_ANSI_U = 0x20;
  public const ushort kVK_ANSI_LeftBracket = 0x21;
  public const ushort kVK_ANSI_I = 0x22;
  public const ushort kVK_ANSI_P = 0x23;
  public const ushort kVK_Return = 0x24;
  public const ushort kVK_ANSI_L = 0x25;
  public const ushort kVK_ANSI_J = 0x26;
  public const ushort kVK_ANSI_Quote = 0x27;
  public const ushort kVK_ANSI_K = 0x28;
  public const ushort kVK_ANSI_Semicolon = 0x29;
  public const ushort kVK_ANSI_Backslash = 0x2A;
  public const ushort kVK_ANSI_Comma = 0x2B;
  public const ushort kVK_ANSI_Slash = 0x2C;
  public const ushort kVK_ANSI_N = 0x2D;
  public const ushort kVK_ANSI_M = 0x2E;
  public const ushort kVK_ANSI_Period = 0x2F;
  public const ushort kVK_Tab = 0x30;
  public const ushort kVK_Space = 0x31;
  public const ushort kVK_ANSI_Grave = 0x32;
  public const ushort kVK_Delete = 0x33;
  public const ushort kVK_Escape = 0x35;
  public const ushort kVK_Command = 0x37;
  public const ushort kVK_Shift = 0x38;
  public const ushort kVK_CapsLock = 0x39;
  public const ushort kVK_Option = 0x3A;
  public const ushort kVK_Control = 0x3B;
  public const ushort kVK_RightCommand = 0x36;
  public const ushort kVK_RightShift = 0x3C;
  public const ushort kVK_RightOption = 0x3D;
  public const ushort kVK_RightControl = 0x3E;
  public const ushort kVK_Function = 0x3F;
  public const ushort kVK_VolumeUp = 0x48;
  public const ushort kVK_VolumeDown = 0x49;
  public const ushort kVK_Mute = 0x4A;
  public const ushort kVK_ANSI_KeypadDecimal = 0x41;
  public const ushort kVK_ANSI_KeypadMultiply = 0x43;
  public const ushort kVK_ANSI_KeypadPlus = 0x45;
  public const ushort kVK_ANSI_KeypadClear = 0x47;
  public const ushort kVK_ANSI_KeypadDivide = 0x4B;
  public const ushort kVK_ANSI_KeypadEnter = 0x4C;
  public const ushort kVK_ANSI_KeypadMinus = 0x4E;
  public const ushort kVK_ANSI_KeypadEquals = 0x51;
  public const ushort kVK_ANSI_Keypad0 = 0x52;
  public const ushort kVK_ANSI_Keypad1 = 0x53;
  public const ushort kVK_ANSI_Keypad2 = 0x54;
  public const ushort kVK_ANSI_Keypad3 = 0x55;
  public const ushort kVK_ANSI_Keypad4 = 0x56;
  public const ushort kVK_ANSI_Keypad5 = 0x57;
  public const ushort kVK_ANSI_Keypad6 = 0x58;
  public const ushort kVK_ANSI_Keypad7 = 0x59;
  public const ushort kVK_ANSI_Keypad8 = 0x5B;
  public const ushort kVK_ANSI_Keypad9 = 0x5C;
  public const ushort kVK_Home = 0x73;
  public const ushort kVK_ForwardDelete = 0x75;
  public const ushort kVK_F1 = 0x7A;
  public const ushort kVK_F2 = 0x78;
  public const ushort kVK_F3 = 0x63;
  public const ushort kVK_F4 = 0x76;
  public const ushort kVK_F5 = 0x60;
  public const ushort kVK_F6 = 0x61;
  public const ushort kVK_F7 = 0x62;
  public const ushort kVK_F8 = 0x64;
  public const ushort kVK_F9 = 0x65;
  public const ushort kVK_F10 = 0x6D;
  public const ushort kVK_End = 0x77;
  public const ushort kVK_F11 = 0x67;
  public const ushort kVK_F12 = 0x6F;
  public const ushort kVK_F13 = 0x69;
  public const ushort kVK_F14 = 0x6B;
  public const ushort kVK_F15 = 0x71;
  public const ushort kVK_F16 = 0x6A;
  public const ushort kVK_F17 = 0x40;
  public const ushort kVK_F18 = 0x4F;
  public const ushort kVK_F19 = 0x50;
  public const ushort kVK_F20 = 0x5A;
  public const ushort kVK_PageUp = 0x74;
  public const ushort kVK_Home_ = 0x73;
  public const ushort kVK_PageDown = 0x79;
  public const ushort kVK_LeftArrow = 0x7B;
  public const ushort kVK_RightArrow = 0x7C;
  public const ushort kVK_DownArrow = 0x7D;
  public const ushort kVK_UpArrow = 0x7E;
  public const ushort kVK_Help = 0x72;

  public const ulong kCGEventFlagMaskShift = 0x00020000;
  public const ulong kCGEventFlagMaskControl = 0x00040000;
  public const ulong kCGEventFlagMaskAlternate = 0x00080000;
  public const ulong kCGEventFlagMaskCommand = 0x00100000;

  private static readonly Dictionary<string, ushort> NamedKeys = new(StringComparer.Ordinal)
  {
    ["enter"] = kVK_Return,
    ["escape"] = kVK_Escape,
    ["tab"] = kVK_Tab,
    ["space"] = kVK_Space,
    ["backspace"] = kVK_Delete,
    ["delete"] = kVK_ForwardDelete,
    ["home"] = kVK_Home,
    ["end"] = kVK_End,
    ["pageup"] = kVK_PageUp,
    ["pagedown"] = kVK_PageDown,
    ["up"] = kVK_UpArrow,
    ["down"] = kVK_DownArrow,
    ["left"] = kVK_LeftArrow,
    ["right"] = kVK_RightArrow,
    ["capslock"] = kVK_CapsLock,
    ["numlock"] = kVK_ANSI_KeypadClear,
    ["menu"] = kVK_Function,
    ["printscreen"] = kVK_Help,
  };

  // US ANSI layout character table: character -> (virtual key, requires shift).
  private static readonly Dictionary<char, (ushort Keycode, bool Shift)> Characters = new()
  {
    ['a'] = (kVK_ANSI_A, false), ['b'] = (kVK_ANSI_B, false), ['c'] = (kVK_ANSI_C, false),
    ['d'] = (kVK_ANSI_D, false), ['e'] = (kVK_ANSI_E, false), ['f'] = (kVK_ANSI_F, false),
    ['g'] = (kVK_ANSI_G, false), ['h'] = (kVK_ANSI_H, false), ['i'] = (kVK_ANSI_I, false),
    ['j'] = (kVK_ANSI_J, false), ['k'] = (kVK_ANSI_K, false), ['l'] = (kVK_ANSI_L, false),
    ['m'] = (kVK_ANSI_M, false), ['n'] = (kVK_ANSI_N, false), ['o'] = (kVK_ANSI_O, false),
    ['p'] = (kVK_ANSI_P, false), ['q'] = (kVK_ANSI_Q, false), ['r'] = (kVK_ANSI_R, false),
    ['s'] = (kVK_ANSI_S, false), ['t'] = (kVK_ANSI_T, false), ['u'] = (kVK_ANSI_U, false),
    ['v'] = (kVK_ANSI_V, false), ['w'] = (kVK_ANSI_W, false), ['x'] = (kVK_ANSI_X, false),
    ['y'] = (kVK_ANSI_Y, false), ['z'] = (kVK_ANSI_Z, false),
    ['0'] = (kVK_ANSI_0, false), ['1'] = (kVK_ANSI_1, false), ['2'] = (kVK_ANSI_2, false),
    ['3'] = (kVK_ANSI_3, false), ['4'] = (kVK_ANSI_4, false), ['5'] = (kVK_ANSI_5, false),
    ['6'] = (kVK_ANSI_6, false), ['7'] = (kVK_ANSI_7, false), ['8'] = (kVK_ANSI_8, false),
    ['9'] = (kVK_ANSI_9, false),
    ['-'] = (kVK_ANSI_Minus, false), ['='] = (kVK_ANSI_Equal, false),
    ['['] = (kVK_ANSI_LeftBracket, false), [']'] = (kVK_ANSI_RightBracket, false),
    ['\\'] = (kVK_ANSI_Backslash, false), [';'] = (kVK_ANSI_Semicolon, false),
    ['\''] = (kVK_ANSI_Quote, false), [','] = (kVK_ANSI_Comma, false),
    ['.'] = (kVK_ANSI_Period, false), ['/'] = (kVK_ANSI_Slash, false),
    ['`'] = (kVK_ANSI_Grave, false), [' '] = (kVK_Space, false),
    ['A'] = (kVK_ANSI_A, true), ['B'] = (kVK_ANSI_B, true), ['C'] = (kVK_ANSI_C, true),
    ['D'] = (kVK_ANSI_D, true), ['E'] = (kVK_ANSI_E, true), ['F'] = (kVK_ANSI_F, true),
    ['G'] = (kVK_ANSI_G, true), ['H'] = (kVK_ANSI_H, true), ['I'] = (kVK_ANSI_I, true),
    ['J'] = (kVK_ANSI_J, true), ['K'] = (kVK_ANSI_K, true), ['L'] = (kVK_ANSI_L, true),
    ['M'] = (kVK_ANSI_M, true), ['N'] = (kVK_ANSI_N, true), ['O'] = (kVK_ANSI_O, true),
    ['P'] = (kVK_ANSI_P, true), ['Q'] = (kVK_ANSI_Q, true), ['R'] = (kVK_ANSI_R, true),
    ['S'] = (kVK_ANSI_S, true), ['T'] = (kVK_ANSI_T, true), ['U'] = (kVK_ANSI_U, true),
    ['V'] = (kVK_ANSI_V, true), ['W'] = (kVK_ANSI_W, true), ['X'] = (kVK_ANSI_X, true),
    ['Y'] = (kVK_ANSI_Y, true), ['Z'] = (kVK_ANSI_Z, true),
    ['!'] = (kVK_ANSI_1, true), ['@'] = (kVK_ANSI_2, true), ['#'] = (kVK_ANSI_3, true),
    ['$'] = (kVK_ANSI_4, true), ['%'] = (kVK_ANSI_5, true), ['^'] = (kVK_ANSI_6, true),
    ['&'] = (kVK_ANSI_7, true), ['*'] = (kVK_ANSI_8, true), ['('] = (kVK_ANSI_9, true),
    [')'] = (kVK_ANSI_0, true), ['_'] = (kVK_ANSI_Minus, true), ['+'] = (kVK_ANSI_Equal, true),
    ['{'] = (kVK_ANSI_LeftBracket, true), ['}'] = (kVK_ANSI_RightBracket, true),
    ['|'] = (kVK_ANSI_Backslash, true), [':'] = (kVK_ANSI_Semicolon, true),
    ['"'] = (kVK_ANSI_Quote, true), ['<'] = (kVK_ANSI_Comma, true),
    ['>'] = (kVK_ANSI_Period, true), ['?'] = (kVK_ANSI_Slash, true),
    ['~'] = (kVK_ANSI_Grave, true),
  };

  // Named punctuation keys map to their unshifted keycodes.
  private static readonly Dictionary<string, ushort> PunctuationNames = new(StringComparer.Ordinal)
  {
    ["minus"] = kVK_ANSI_Minus,
    ["equals"] = kVK_ANSI_Equal,
    ["leftbracket"] = kVK_ANSI_LeftBracket,
    ["rightbracket"] = kVK_ANSI_RightBracket,
    ["backslash"] = kVK_ANSI_Backslash,
    ["semicolon"] = kVK_ANSI_Semicolon,
    ["quote"] = kVK_ANSI_Quote,
    ["comma"] = kVK_ANSI_Comma,
    ["period"] = kVK_ANSI_Period,
    ["slash"] = kVK_ANSI_Slash,
    ["grave"] = kVK_ANSI_Grave,
    ["plus"] = kVK_ANSI_Equal,
  };

  /// <summary>Maps a normalized key name to a macOS virtual key code.</summary>
  public static bool TryGetVirtualKey(string normalizedName, out ushort virtualKey)
  {
    if (NamedKeys.TryGetValue(normalizedName, out virtualKey) ||
        PunctuationNames.TryGetValue(normalizedName, out virtualKey))
    {
      return true;
    }

    if (normalizedName.Length is >= 2 and <= 3 && normalizedName[0] == 'f' &&
        int.TryParse(normalizedName.AsSpan(1), out var number) && number is >= 1 and <= 20)
    {
      virtualKey = FunctionKey((ushort)number);
      return true;
    }

    return false;
  }

  /// <summary>Maps a character to a US-layout virtual key code and shift requirement.</summary>
  public static bool TryGetCharacter(char character, out ushort virtualKey, out bool requiresShift)
  {
    if (Characters.TryGetValue(character, out var entry))
    {
      virtualKey = entry.Keycode;
      requiresShift = entry.Shift;
      return true;
    }

    virtualKey = 0;
    requiresShift = false;
    return false;
  }

  /// <summary>The left virtual key code for a chord modifier.</summary>
  public static ushort GetModifierVirtualKey(ModifierKey modifier) => modifier switch
  {
    ModifierKey.Shift => kVK_Shift,
    ModifierKey.Control => kVK_Control,
    ModifierKey.Alt => kVK_Option,
    ModifierKey.Meta => kVK_Command,
    _ => throw new ArgumentOutOfRangeException(nameof(modifier), modifier, null),
  };

  /// <summary>The CoreGraphics flag mask for a chord modifier (for CGEventSetFlags).</summary>
  public static ulong GetModifierFlag(ModifierKey modifier) => modifier switch
  {
    ModifierKey.Shift => kCGEventFlagMaskShift,
    ModifierKey.Control => kCGEventFlagMaskControl,
    ModifierKey.Alt => kCGEventFlagMaskAlternate,
    ModifierKey.Meta => kCGEventFlagMaskCommand,
    _ => throw new ArgumentOutOfRangeException(nameof(modifier), modifier, null),
  };

  private static ushort FunctionKey(ushort number) => number switch
  {
    1 => kVK_F1,
    2 => kVK_F2,
    3 => kVK_F3,
    4 => kVK_F4,
    5 => kVK_F5,
    6 => kVK_F6,
    7 => kVK_F7,
    8 => kVK_F8,
    9 => kVK_F9,
    10 => kVK_F10,
    11 => kVK_F11,
    12 => kVK_F12,
    13 => kVK_F13,
    14 => kVK_F14,
    15 => kVK_F15,
    16 => kVK_F16,
    17 => kVK_F17,
    18 => kVK_F18,
    19 => kVK_F19,
    20 => kVK_F20,
    _ => 0,
  };

  private const ushort kVK_ANSI_Minus = 0x1B;
}
