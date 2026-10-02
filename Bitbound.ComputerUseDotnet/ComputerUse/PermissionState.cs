namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>Permission states reported by <see cref="PermissionStatus"/>.</summary>
public enum PermissionState
{
  /// <summary>The platform does not gate this capability behind a permission.</summary>
  NotRequired,

  /// <summary>The permission has been granted.</summary>
  Granted,

  /// <summary>The permission has not been granted yet; user action is required.</summary>
  NotGranted,

  /// <summary>The grant is managed by an external broker and may prompt on first use.</summary>
  PromptOnUse,

  /// <summary>The state could not be determined.</summary>
  Unknown,
}
