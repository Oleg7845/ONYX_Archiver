namespace UpdateAgent.Domain.Interfaces;

/// <summary>
/// Defines the core contract for the application update lifecycle.
/// Responsible for orchestrating backup, extraction, and file replacement procedures.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Executes the full update sequence asynchronously.
    /// </summary>
    /// <param name="progress">
    /// An optional provider to report the percentage of completion (0.0 to 100.0).
    /// Used for updating UI progress bars.
    /// </param>
    /// <param name="status">
    /// An optional provider to report human-readable status messages 
    /// (e.g., "Backing up files...", "Extracting update...").
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous update operation.</returns>
    /// <exception cref="System.IO.IOException">Thrown when file access is denied or storage is full.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when the agent lacks sufficient permissions.</exception>
    Task RunUpdateAsync(
        IProgress<double>? progress = null,
        IProgress<string>? status = null);
}