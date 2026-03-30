using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using UpdateServer.Infrastructure.Data;

namespace UpdateServer.Infrastructure;

/// <summary>
/// Provides a way for Entity Framework Core CLI tools (migrations) to create 
/// a <see cref="AppDbContext"/> instance at design time.
/// This is essential when the DbContext is in a separate library from the startup project.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>
    /// Creates a new database context instance for design-time operations.
    /// </summary>
    /// <param name="args">Command-line arguments passed by the EF CLI.</param>
    /// <returns>A configured <see cref="AppDbContext"/> using PostgreSQL.</returns>
    public AppDbContext CreateDbContext(string[] args)
    {
        // 1. Determine the current environment (Development, Staging, Production).
        // This ensures the correct appsettings file is loaded during migration generation.
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        // 2. Build a configuration object manually.
        // Since we are at design time, the standard WebApplication host is not running.
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // 3. Extract the connection string.
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // 4. Configure the DbContext options to use Npgsql (PostgreSQL).
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        // 5. Return the initialized context.
        return new AppDbContext(optionsBuilder.Options);
    }
}