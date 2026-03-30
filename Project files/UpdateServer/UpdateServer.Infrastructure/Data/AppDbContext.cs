using Microsoft.EntityFrameworkCore;
using UpdateServer.Domain.Entities;

namespace UpdateServer.Infrastructure.Data;

/// <summary>
/// The primary database context for the Update Server.
/// Manages the mapping between domain entities and PostgreSQL tables.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Gets the collection of applications. 
    /// Use <see cref="Set{T}"/> to ensure non-nullable reference types are handled correctly by EF Core.
    /// </summary>
    public DbSet<Application> Applications => Set<Application>();

    /// <summary>
    /// Gets the collection of deployment channels (e.g., Stable, Beta).
    /// </summary>
    public DbSet<Channel> Channels => Set<Channel>();

    /// <summary>
    /// Gets the collection of software releases.
    /// </summary>
    public DbSet<Release> Releases => Set<Release>();

    /// <summary>
    /// Gets the collection of metadata for physical update files.
    /// </summary>
    public DbSet<ReleaseFile> ReleaseFiles => Set<ReleaseFile>();

    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/>.
    /// </summary>
    /// <param name="options">The options to be used by this context (provided via DI in Program.cs).</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        // Constructor is kept simple to support standard Dependency Injection patterns.
    }

    /// <summary>
    /// Configures the database schema and entity relationships using Fluent API.
    /// This is preferred over Data Annotations for complex production schemas.
    /// </summary>
    /// <param name="builder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 1. Application -> Channels (One-to-Many)
        // An application acts as a root for multiple update tracks.
        builder.Entity<Application>()
            .HasMany(a => a.Channels)
            .WithOne(c => c.Application)
            .HasForeignKey(c => c.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade); // Deleting an app removes its channels.

        // 2. Channel -> Releases (One-to-Many)
        // Each channel manages its own independent history of versions.
        builder.Entity<Channel>()
            .HasMany(c => c.Releases)
            .WithOne(r => r.Channel)
            .HasForeignKey(r => r.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        // 3. Release -> ReleaseFiles (One-to-Many)
        // A release version can consist of one or more physical binaries.
        builder.Entity<Release>()
            .HasMany(r => r.Files)
            .WithOne(f => f.Release)
            .HasForeignKey(f => f.ReleaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexing for performance:
        // Ensure that searching for an application by Name is fast at scale.
        builder.Entity<Application>()
            .HasIndex(a => a.Name)
            .IsUnique();
    }
}