namespace Bitbound.ComputerUseDotnet.ComputerUse;

/// <summary>
/// A position as a fraction of the virtual screen, where (0, 0) is the top-left corner of
/// the union of all displays and (1, 1) is the bottom-right corner. Fractions name the same
/// position whatever size a capture of that screen happens to be.
/// </summary>
public readonly record struct NormalizedPoint(double X, double Y);
