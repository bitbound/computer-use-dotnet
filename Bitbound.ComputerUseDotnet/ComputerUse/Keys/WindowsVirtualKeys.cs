namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>Windows virtual-key codes for named keys and chord modifiers (Win32 VK_* constants).</summary>
public static class WindowsVirtualKeys
{
  public const ushort VK_APPS = 0x5D;
  public const ushort VK_BACK = 0x08;
  public const ushort VK_CAPITAL = 0x14;
  public const ushort VK_CONTROL = 0x11;
  public const ushort VK_DELETE = 0x2E;
  public const ushort VK_DOWN = 0x28;
  public const ushort VK_END = 0x23;
  public const ushort VK_ESCAPE = 0x1B;
  public const ushort VK_HELP = 0x2F;
  public const ushort VK_HOME = 0x24;
  public const ushort VK_INSERT = 0x2D;
  public const ushort VK_LCONTROL = 0xA2;
  public const ushort VK_LEFT = 0x25;
  public const ushort VK_LMENU = 0xA4;
  public const ushort VK_LSHIFT = 0xA0;
  public const ushort VK_LWIN = 0x5B;
  public const ushort VK_MENU = 0x12;
  public const ushort VK_NEXT = 0x22;
  public const ushort VK_NUMLOCK = 0x90;
  public const ushort VK_PAUSE = 0x13;
  public const ushort VK_PRINT = 0x2A;
  public const ushort VK_PRIOR = 0x21;
  public const ushort VK_RCONTROL = 0xA3;
  public const ushort VK_RETURN = 0x0D;
  public const ushort VK_RIGHT = 0x27;
  public const ushort VK_RMENU = 0xA5;
  public const ushort VK_RSHIFT = 0xA1;
  public const ushort VK_SCROLL = 0x91;
  public const ushort VK_SHIFT = 0x10;
  public const ushort VK_SNAPSHOT = 0x2C;
  public const ushort VK_SPACE = 0x20;
  public const ushort VK_TAB = 0x09;
  public const ushort VK_UP = 0x26;

  private static readonly HashSet<string> ExtendedKeys = new(StringComparer.Ordinal)
  {
    "up", "down", "left", "right", "home", "end", "pageup", "pagedown", "insert", "delete", "printscreen", "menu",
  };
  private static readonly Dictionary<string, ushort> NamedKeys = new(StringComparer.Ordinal)
  {
    ["enter"] = VK_RETURN,
    ["escape"] = VK_ESCAPE,
    ["tab"] = VK_TAB,
    ["space"] = VK_SPACE,
    ["backspace"] = VK_BACK,
    ["delete"] = VK_DELETE,
    ["insert"] = VK_INSERT,
    ["home"] = VK_HOME,
    ["end"] = VK_END,
    ["pageup"] = VK_PRIOR,
    ["pagedown"] = VK_NEXT,
    ["up"] = VK_UP,
    ["down"] = VK_DOWN,
    ["left"] = VK_LEFT,
    ["right"] = VK_RIGHT,
    ["capslock"] = VK_CAPITAL,
    ["numlock"] = VK_NUMLOCK,
    ["scrolllock"] = VK_SCROLL,
    ["pause"] = VK_PAUSE,
    ["printscreen"] = VK_SNAPSHOT,
    ["menu"] = VK_APPS,
  };

  /// <summary>The left virtual-key code for a chord modifier.</summary>
  public static ushort GetModifierVirtualKey(ModifierKey modifier) => modifier switch
  {
    ModifierKey.Shift => VK_LSHIFT,
    ModifierKey.Control => VK_LCONTROL,
    ModifierKey.Alt => VK_LMENU,
    ModifierKey.Meta => VK_LWIN,
    _ => throw new ArgumentOutOfRangeException(nameof(modifier), modifier, null),
  };

  /// <summary>Whether the named key requires KEYEVENTF_EXTENDEDKEY.</summary>
  public static bool IsExtended(string normalizedName) => ExtendedKeys.Contains(normalizedName);

  /// <summary>Maps a normalized key name to its Win32 virtual-key code.</summary>
  public static bool TryGetVirtualKey(string normalizedName, out ushort virtualKey) =>
    NamedKeys.TryGetValue(normalizedName, out virtualKey) ||
    TryGetFunctionKey(normalizedName, out virtualKey);

  private static bool TryGetFunctionKey(string name, out ushort virtualKey)
  {
    virtualKey = 0;

    if (name.Length < 2 || name[0] != 'f')
    {
      return false;
    }

    if (!int.TryParse(name.AsSpan(1), out var number) || number is < 1 or > 24)
    {
      return false;
    }

    virtualKey = (ushort)(0x6F + number);
    return true;
  }
}
