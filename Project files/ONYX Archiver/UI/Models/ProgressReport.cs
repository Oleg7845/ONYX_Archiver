namespace OnyxArchiver.UI.Models;

/// <summary>
/// Represents a snapshot of the current operation's progress.
/// Used to provide real-time feedback to the user interface during long-running tasks 
/// such as file compression, encryption, or extraction.
/// </summary>
public class ProgressReport
{
    /// <summary>
    /// The overall completion percentage of the current task (e.g., 45.5%).
    /// Typically used to drive a ProgressBar.Value.
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// Amount of data already processed (downloaded/compressed) in Megabytes.
    /// </summary>
    public double DownloadedMb { get; set; }

    /// <summary>
    /// The total expected size of the operation in Megabytes.
    /// Used for "X of Y MB" labels in the UI.
    /// </summary>
    public double TotalMb { get; set; }

    /// <summary>
    /// Estimated time remaining until the task completes (ETA).
    /// </summary>
    public TimeSpan RemainingTime { get; set; }

    /// <summary>
    /// Current transfer or processing speed in Megabits per second.
    /// </summary>
    public double SpeedMbps { get; set; }
}