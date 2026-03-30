namespace UpdateAgent.Domain.Interfaces;

/// <summary>
/// Defines the core contract for managing application paths and metadata.
/// This service acts as a central provider for directory structures required during the update process.
/// </summary>
public interface IAppService
{
    /// <summary>
    /// Gets the absolute file path to the Update Agent's own executable.
    /// </summary>
    string ExePath { get; }

    /// <summary>
    /// Gets the display name or identifier of the target application being updated.
    /// </summary>
    string AppName { get; }

    /// <summary>
    /// Gets the absolute file path to the target application's main executable file.
    /// </summary>
    string AppExePath { get; }

    /// <summary>
    /// Gets the absolute path to the root directory where the target application is installed.
    /// </summary>
    string AppFolderPath { get; }

    /// <summary>
    /// Gets the absolute path to the temporary update package or installer file.
    /// </summary>
    string UpdateFilePath { get; }

    /// <summary>
    /// Gets the absolute path to the directory designated for storing backups before applying updates.
    /// </summary>
    string BackupFolderPath { get; }
}