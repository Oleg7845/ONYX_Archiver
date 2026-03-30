namespace OnyxArchiver.Core.Models.Archive;

/// <summary>
/// Represents a snapshot of the current operation's progress.
/// Used to provide real-time feedback to the user interface during long-running tasks 
/// such as file compression, encryption, or extraction.
/// </summary>
public class ProgressReport
{
    /// <summary>
    /// Gets or sets the overall completion percentage.
    /// Typically ranges from 0.0 to 100.0.
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// Gets or sets the name or path of the file currently being processed.
    /// Useful for displaying status messages like "Processing: document.pdf".
    /// </summary>
    public string CurrentFile { get; set; } = string.Empty;
}
