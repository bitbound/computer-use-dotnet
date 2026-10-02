namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>Permission state for screen capture and input simulation on the current platform.</summary>
public sealed record PermissionStatus(
  string BackendName,
  PermissionState ScreenCapture,
  PermissionState InputSimulation,
  string Details);
