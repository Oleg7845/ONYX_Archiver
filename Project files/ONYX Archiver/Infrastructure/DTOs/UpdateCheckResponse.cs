namespace OnyxArchiver.Infrastructure.DTOs;

/// <summary>
/// Represents the result of an update availability check.
/// Contains the metadata required to verify, download, and install a new version.
/// </summary>
public class UpdateCheckResponse
{
    /// <summary>Gets or sets a value indicating whether a newer version is available on the server.</summary>
    public bool HasUpdate { get; set; }

    /// <summary>Gets or sets the semantic version string of the available update (e.g., "1.2.0").</summary>
    public string? Version { get; set; }

    /// <summary>Gets or sets the direct HTTPS URL to the update binary or installer package.</summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the SHA-256 hash of the update file. 
    /// Used to verify file integrity immediately after the download completes.
    /// </summary>
    public string? Hash { get; set; }

    /// <summary>
    /// Gets or sets the Ed25519 digital signature of the update package.
    /// This is verified against the 'ServerSignaturePublicKey' stored in the application.
    /// </summary>
    public string? Signature { get; set; }

    /// <summary>Gets or sets the total size of the update package in bytes. Useful for progress bars.</summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this update is critical for security or stability.
    /// If true, the application may restrict access to the vault until the update is applied.
    /// </summary>
    public bool Mandatory { get; set; }
}