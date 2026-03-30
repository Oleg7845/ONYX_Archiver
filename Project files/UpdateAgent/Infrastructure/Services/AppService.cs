using UpdateAgent.Domain.Interfaces;
using System.IO;

namespace UpdateAgent.Infrastructure.Services;

/// <summary>
/// Infrastructure-level implementation of the <see cref="IAppService"/> interface.
/// Responsible for path resolution and environmental state management during the update lifecycle.
/// </summary>
public class AppService : IAppService
{
    private readonly string _exePath;
    private string _backupFolderPath = string.Empty;

    /// <inheritdoc/>
    public string ExePath => _exePath;

    /// <inheritdoc/>
    public string AppName { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public string AppExePath { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public string AppFolderPath { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public string UpdateFilePath { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public string BackupFolderPath => _backupFolderPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppService"/> class.
    /// Captures the current process path immediately upon instantiation.
    /// </summary>
    public AppService()
    {
        // Environment.ProcessPath is used to reliably identify the location of the Update Agent itself.
        _exePath = Environment.ProcessPath!;
    }

    /// <summary>
    /// Populates service properties with specific application data and ensures 
    /// the necessary directory structure for backups is created.
    /// </summary>
    /// <param name="appName">The display name of the target application.</param>
    /// <param name="appExePath">The full path to the target application's executable.</param>
    /// <param name="updateFilePath">The path where the update package is currently located.</param>
    /// <exception cref="ArgumentNullException">Thrown if input paths are null or empty.</exception>
    /// <exception cref="IOException">Thrown if the backup directory cannot be created.</exception>
    public void AddPropertiesValues(string appName, string appExePath, string updateFilePath)
    {
        // Logic: The backup folder is created two levels up from the update file path 
        // to maintain a clean structure within the parent 'ONYX Archiver' workspace.
        _backupFolderPath = Path.Combine(
            Path.GetDirectoryName(Path.GetDirectoryName(updateFilePath))!,
            "Backup");

        // Ensure the environment is ready for the backup operation.
        if (!Directory.Exists(_backupFolderPath))
        {
            Directory.CreateDirectory(_backupFolderPath);
        }

        AppName = appName;
        AppExePath = appExePath;

        // Derives the target application's root directory from its executable path.
        AppFolderPath = Path.GetDirectoryName(appExePath)!;

        UpdateFilePath = updateFilePath;
    }
}