using OnyxArchiver.Infrastructure.DTOs;
using OnyxArchiver.UI.Models;

namespace OnyxArchiver.Domain.Interfaces;

/// <summary>
/// Defines the contract for managing application updates.
/// Handles the discovery, downloading, and installation of new software versions.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Checks the remote update server for a newer version of the application.
    /// </summary>
    /// <returns>
    /// An <see cref="UpdateCheckResponse"/> containing version details and release notes if an update is available; 
    /// otherwise, null.
    /// </returns>
    /// <remarks>
    /// This should ideally be called on application startup or via a user-triggered "Check for Updates" button.
    /// </remarks>
    Task<UpdateCheckResponse?> CheckAsync();

    /// <summary>
    /// Downloads and initiates the installation of a specific update.
    /// </summary>
    /// <param name="update">The update metadata returned from a successful check.</param>
    /// <param name="progress">An optional provider to report download percentage and status to the UI.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous update process.</returns>
    /// <exception cref="HttpRequestException">Thrown if the update server is unreachable.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the update package signature fails verification.</exception>
    Task UpdateAsync(
        UpdateCheckResponse update,
        IProgress<ProgressReport>? progress = null);
}