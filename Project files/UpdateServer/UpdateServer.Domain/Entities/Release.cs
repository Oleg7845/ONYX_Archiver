namespace UpdateServer.Domain.Entities;

/// <summary>
/// Represents a specific version release of an application within a deployment channel.
/// Contains metadata about the version and orchestrates the collection of associated binary files.
/// </summary>
public class Release
{
    /// <summary>
    /// Gets or sets the unique primary key for the release record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the semantic version string (e.g., "1.2.0.4").
    /// This string is parsed by the <see cref="UpdateService"/> for numerical comparison.
    /// </summary>
    public string Version { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether this update is critical.
    /// When true, the Update Agent should force the user to update before allowing the app to run.
    /// </summary>
    public bool IsMandatory { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when this release was published.
    /// Stored in UTC for global synchronization.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the foreign key linking this release to a specific <see cref="Entities.Channel"/>.
    /// </summary>
    public Guid ChannelId { get; set; }

    /// <summary>
    /// Navigation property to the parent deployment channel (e.g., Stable or Beta).
    /// </summary>
    public Channel Channel { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of binary files associated with this release.
    /// A single release may contain multiple files (e.g., installers for different architectures).
    /// </summary>
    public ICollection<ReleaseFile> Files { get; set; } = new List<ReleaseFile>();
}