namespace UpdateServer.Domain.Entities;

/// <summary>
/// Represents a unique software product managed by the update system.
/// Acts as the root entity for grouping release channels and version history.
/// </summary>
public class Application
{
    /// <summary>
    /// Gets or sets the unique primary key for the application.
    /// Using <see cref="Guid"/> ensures global uniqueness and prevents ID enumeration attacks.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique internal name or identifier of the application (e.g., "OnyxArchiver").
    /// This name is used as a lookup key in API requests.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date and time when the application record was registered in the system.
    /// Always stored in UTC to ensure consistency across different server time zones.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the collection of deployment channels (e.g., Stable, Beta, Nightly) 
    /// associated with this application.
    /// </summary>
    public ICollection<Channel> Channels { get; set; } = new List<Channel>();
}