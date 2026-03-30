using OnyxArchiver.Domain.Interfaces;
using System.IO;

namespace OnyxArchiver.Infrastructure.Services;

/// <summary>
/// Provides a centralized implementation for application-wide settings and environment paths.
/// Ensures all required filesystem structures are initialized upon service instantiation.
/// </summary>
public class AppService : IAppService
{
    private readonly string _exePath;
    private readonly string _localAppDataPath;

    public string AppName => "ONYX Archiver";
    public string AppVersion => "1.0.0";

    /// <summary>
    /// Base64 encoded public key for verifying update package signatures (Ed25519).
    /// </summary>
    public string ServerSignaturePublicKey => "rZpNjdQZBspDuEoRM2SmeZDgQOna/aEn9JKq7ZfbZAM=";

    public string ServerUrl => "http://localhost:5000";
    public string ExePath => _exePath;

    // --- Filesystem Paths ---

    public string LogsDirectoryPath => Path.Combine(_localAppDataPath, "Logs");

    public string UpdateAgentDirectoryPath => Path.Combine(Path.GetDirectoryName(ExePath)!, "UpdateAgent");

    public string UpdateAgentExePath => Path.Combine(UpdateAgentDirectoryPath, "UpdateAgent.exe");

    public string UpdateFilePath => Path.Combine(_localAppDataPath, "Update Agent", "Update", "update.zip");

    public string DbFilePath => Path.Combine(_localAppDataPath, "Database", "onyx.db");

    public string SettingsFilePath => Path.Combine(_localAppDataPath, "settings.json");

    /// <summary>
    /// Initializes environment paths and enforces directory creation.
    /// </summary>
    public AppService()
    {
        // Capture the current process location for relative path resolution
        _exePath = Environment.ProcessPath!;

        // Resolve the OS-specific local application data folder (e.g., %AppData%/Local/ONYX Archiver)
        _localAppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppName);

        // Pre-emptively create the directory structure to avoid IO exceptions in other services
        CreateDirectories();
    }

    /// <summary>
    /// Iterates through all critical file and folder paths, creating parent directories where necessary.
    /// This prevents "DirectoryNotFoundException" in database, logging, and update services.
    /// </summary>
    private void CreateDirectories()
    {
        string[] paths = [
            LogsDirectoryPath,
            DbFilePath,
            SettingsFilePath,
            UpdateFilePath,
            UpdateAgentExePath];

        foreach (var path in paths.Distinct())
        {
            // Logic to distinguish between a directory path and a file path
            var dirPath = string.IsNullOrEmpty(Path.GetExtension(path))
                ? path
                : Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }
    }
}