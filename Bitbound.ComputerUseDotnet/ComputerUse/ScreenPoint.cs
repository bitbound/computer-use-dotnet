namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>
/// A point in virtual-screen coordinate space: logical pixels of the composite
/// (union of all displays) with the top-left corner of the union as the origin.
/// This is the same space as a full-virtual-screen <c>take_screenshot</c> image.
/// </summary>
public readonly record struct ScreenPoint(int X, int Y);
