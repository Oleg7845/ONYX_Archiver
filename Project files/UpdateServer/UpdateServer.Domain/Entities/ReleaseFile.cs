namespace UpdateServer.Domain.Entities;

/// <summary>
/// Represents a physical binary file associated with a specific application release.
/// Contains cryptographic metadata required for secure delivery and verification.
/// </summary>
public class ReleaseFile
{
    /// <summary>
    /// Gets or sets the unique primary key for the release file record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the full name of the file as stored on the file system (e.g., "update_v1.0.1.zip").
    /// This name is used to construct download URLs in the <see cref="UpdateService"/>.
    /// </summary>
    public string FileName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the SHA-512 cryptographic hash of the file.
    /// Essential for the client to perform post-download integrity checks.
    /// </summary>
    public string Hash { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Ed25519 digital signature of the file.
    /// Used by the client to verify that the file was officially signed by the vendor.
    /// </summary>
    public string Signature { get; set; } = null!;

    /// <summary>
    /// Gets or sets the total size of the file in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the category of the update file (e.g., "full" for complete archive, "delta" for patches).
    /// Default is "full".
    /// </summary>
    public string FileType { get; set; } = "full";

    /// <summary>
    /// Gets or sets the foreign key linking this file to its parent <see cref="Entities.Release"/>.
    /// </summary>
    public Guid ReleaseId { get; set; }

    /// <summary>
    /// Navigation property to the associated <see cref="Entities.Release"/>.
    /// </summary>
    public Release Release { get; set; } = null!;
}