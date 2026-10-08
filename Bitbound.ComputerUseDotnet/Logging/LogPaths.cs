namespace Bitbound.ComputerUseDotnet.Logging;

/// <summary>
/// Single source of truth for where the rolling log file lives, so the file logger and the
/// <c>read_logs</c> tool agree on the path without either one owning the computation.
/// </summary>
internal static class LogPaths
{
  public static string CurrentFile => Path.Combine(LogDirectory, "computer-use-dotnet.log");

  public static string LogDirectory { get; } = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "Bitbound", "ComputerUseDotnet", "Logs");
}
