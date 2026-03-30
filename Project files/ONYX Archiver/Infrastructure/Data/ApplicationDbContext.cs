using Microsoft.EntityFrameworkCore;
using OnyxArchiver.Domain.Entities;

namespace OnyxArchiver.Infrastructure.Data;

/// <summary>
/// The primary database context for the Onyx Archiver application.
/// Manages persistent storage for user profiles and peer information using Entity Framework Core and SQLite.
/// </summary>
/// <remarks>
/// This context is responsible for configuring the database connection, 
/// defining schema constraints, and managing transactions for domain entities.
/// </remarks>
public class ApplicationDbContext : DbContext
{
    private readonly string _dbPath;

    /// <summary> Gets the set of local user profiles (identities) stored in the database. </summary>
    public DbSet<UserEntity> Users => Set<UserEntity>();

    /// <summary> Gets the set of known peers (contacts) for secure key exchange. </summary>
    public DbSet<PeerEntity> Peers => Set<PeerEntity>();

    /// <summary>
    /// Initializes a new instance of the context with a specific path to the SQLite database file.
    /// </summary>
    /// <param name="db">The absolute or relative path to the .db file.</param>
    public ApplicationDbContext(string db) => _dbPath = db;

    /// <summary>
    /// Configures the context to use the SQLite provider with shared cache enabled for better performance.
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={_dbPath};Mode=ReadWriteCreate;");
    }

    /// <summary>
    /// Configures the database schema, including keys, constraints, and indexes using Fluent API.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuration for UserEntity: Ensures username uniqueness and length constraints.
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Username).IsUnique();
        });

        // Configuration for PeerEntity: Sets primary keys and field constraints for the address book.
        modelBuilder.Entity<PeerEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
        });
    }
}
