namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>
/// A point in virtual-screen space: pixels of the composite (union of all displays) with the
/// top-left corner of the union as the origin. Backends translate between this and native layout
/// coordinates at their own boundary.
/// </summary>
public readonly record struct ScreenPoint(int X, int Y);
