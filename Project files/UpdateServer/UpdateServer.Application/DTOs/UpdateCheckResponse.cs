namespace UpdateServer.Application.DTOs;

/// <summary>
/// Data Transfer Object representing the result of an update availability check.
/// This object is serialized and sent to the client (Update Agent).
/// </summary>
public class UpdateCheckResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether a newer version of the application is available.
    /// </summary>
    public bool HasUpdate { get; set; }

    /// <summary>
    /// Gets or sets the version string of the available update (e.g., "1.2.5").
    /// Returns null if no update is found.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the absolute URL from which the update package can be downloaded.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the cryptographic hash (e.g., SHA-512) of the update file.
    /// Used by the client to verify file integrity after download.
    /// </summary>
    public string? Hash { get; set; }

    /// <summary>
    /// Gets or sets the digital signature of the update package (Ed25519).
    /// Used to verify that the update was issued by a trusted authority.
    /// </summary>
    public string? Signature { get; set; }

    /// <summary>
    /// Gets or sets the size of the update package in bytes.
    /// Allows the client to show remaining download time or check for sufficient disk space.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this update is mandatory.
    /// If true, the client should prevent the application from starting until the update is applied.
    /// </summary>
    public bool Mandatory { get; set; }
}