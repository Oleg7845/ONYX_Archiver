using UpdateServer.Application.DTOs;
using UpdateServer.Domain.Repositories;

namespace UpdateServer.Application.Services;

/// <summary>
/// Domain service responsible for update logic orchestration.
/// Handles version comparison, release filtering by channels, and metadata mapping.
/// </summary>
public class UpdateService
{
    private readonly IUpdateRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateService"/>.
    /// </summary>
    /// <param name="repository">Data access layer for applications and releases.</param>
    public UpdateService(IUpdateRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Checks if a newer version of the specified application is available for the given channel.
    /// </summary>
    /// <param name="applicationName">The unique identifier of the application.</param>
    /// <param name="clientVersion">The version currently installed on the client machine.</param>
    /// <param name="channel">The target update channel (e.g., "stable", "beta").</param>
    /// <returns>An <see cref="UpdateCheckResponse"/> containing update details or status.</returns>
    /// <exception cref="Exception">Thrown when the application or release data is inconsistent.</exception>
    public async Task<UpdateCheckResponse> CheckAsync(string applicationName, string clientVersion, string channel = "stable")
    {
        // 1. Fetch application metadata
        var app = await _repository.GetApplicationByNameAsync(applicationName);
        if (app == null)
            throw new Exception($"Application '{applicationName}' was not found in the registry.");

        // 2. Retrieve the most recent release for the specified deployment channel
        var release = await _repository.GetLatestReleaseAsync(app.Id, channel);
        if (release == null)
        {
            // No releases available for this specific channel yet.
            return new UpdateCheckResponse { HasUpdate = false };
        }

        // 3. Version semantic validation
        // Using System.Version for robust "Major.Minor.Build.Revision" comparison logic.
        if (!Version.TryParse(clientVersion, out var clientVer))
            throw new Exception("The client provided an invalid version format.");

        if (!Version.TryParse(release.Version, out var serverVer))
            throw new Exception($"Server database contains an invalid version string for release: {release.Version}");

        // 4. Comparison Logic
        // If the server version is not strictly greater than the client version, no update is needed.
        if (serverVer <= clientVer)
        {
            return new UpdateCheckResponse { HasUpdate = false };
        }

        // 5. File Mapping
        // Every release must have at least one associated binary file (e.g., a .zip archive).
        var file = release.Files.FirstOrDefault();
        if (file == null)
            throw new Exception($"Consistency error: Release {release.Version} exists but has no associated files.");

        // 6. Response Construction
        return new UpdateCheckResponse
        {
            HasUpdate = true,
            Version = release.Version,
            // The URL points to the static file server path configured in Program.cs
            Url = $"/files/{file.FileName}",
            Hash = file.Hash,
            Signature = file.Signature,
            Size = file.Size,
            Mandatory = release.IsMandatory
        };
    }
}