namespace OnyxArchiver.Domain.Interfaces;

/// <summary>
/// Provides global application constants, environment paths, and configuration metadata.
/// Acts as the environmental "Source of Truth" for all filesystem-dependent services.
/// </summary>
public interface IAppService
{
    /// <summary>Gets the formal name of the application.</summary>
    string AppName { get; }

    /// <summary>Gets the current SemVer string of the running assembly.</summary>
    string AppVersion { get; }

    /// <summary>
    /// Gets the Ed25519 public key used to verify the digital signatures of 
    /// update packages received from the remote server.
    /// </summary>
    string ServerSignaturePublicKey { get; }

    /// <summary>Gets the base URL for the remote update and metadata server.</summary>
    string ServerUrl { get; }

    /// <summary>Gets the full path to the currently running executable.</summary>
    string ExePath { get; }

    /// <summary>Gets the directory path where diagnostic logs are stored.</summary>
    string LogsDirectoryPath { get; }

    /// <summary>Gets the directory containing the independent update orchestrator.</summary>
    string UpdateAgentDirectoryPath { get; }

    /// <summary>Gets the path to the executable responsible for applying updates while the main app is closed.</summary>
    string UpdateAgentExePath { get; }

    /// <summary>Gets the temporary path where downloaded update binaries are staged.</summary>
    string UpdateFilePath { get; }

    /// <summary>Gets the absolute path to the SQLite database file (the Vault).</summary>
    string DbFilePath { get; }

    /// <summary>Gets the path to the JSON/XML file containing non-sensitive application preferences.</summary>
    string SettingsFilePath { get; }
}