namespace UpdateServer.Domain.Entities;

/// <summary>
/// Represents a specific deployment pipeline or release track for an application.
/// Common examples include "stable", "beta", "preview", or "dev".
/// </summary>
public class Channel
{
    /// <summary>
    /// Gets or sets the unique primary key for the deployment channel.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the channel.
    /// Default is "stable". This value is used by the <see cref="UpdateController"/> 
    /// to filter which releases are visible to the client.
    /// </summary>
    public string Name { get; set; } = "stable";

    /// <summary>
    /// Gets or sets the foreign key linking this channel to its parent <see cref="Entities.Application"/>.
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Navigation property to the parent application.
    /// </summary>
    public Application Application { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of releases published within this specific channel.
    /// This allows for version history to be tracked independently per track (e.g., Beta vs Stable).
    /// </summary>
    public ICollection<Release> Releases { get; set; } = new List<Release>();
}