using OnyxArchiver.Core.Models.Archive;
using OnyxArchiver.Core.Models.Archive.VirtualCatalog;

namespace OnyxArchiver.Domain.Interfaces;

/// <summary>
/// Orchestrates the creation, inspection, and extraction of secure archives (.onyx).
/// Acts as the primary coordinator for compression, metadata management, and targeted encryption.
/// </summary>
public interface IArchiveService
{
    /// <summary>
    /// Compresses and encrypts a directory or file specifically for a chosen recipient.
    /// </summary>
    /// <param name="peerId">The database ID of the peer whose public keys will seal the archive.</param>
    /// <param name="sourcePath">The local filesystem path to the data to be protected.</param>
    /// <param name="destinationPath">The directory where the resulting encrypted archive will be saved.</param>
    /// <param name="archiveName">The final filename (e.g., "ProjectData.onyx").</param>
    /// <param name="cancellationTokenSrc">Token to allow the user to safely abort the encryption/compression process.</param>
    /// <param name="progress">Reporter to send real-time percentage and status updates to the UI.</param>
    /// <returns>A task representing the asynchronous archival process.</returns>
    Task CreateArchiveAsync(
        int peerId,
        string sourcePath,
        string destinationPath,
        string archiveName,
        CancellationTokenSource? cancellationTokenSrc = null,
        IProgress<ProgressReport>? progress = null);

    /// <summary>
    /// Reads the encrypted header of an archive to reconstruct the file structure 
    /// without decrypting the actual file contents.
    /// </summary>
    /// <param name="archivePath">The path to the .onyx file.</param>
    /// <returns>A <see cref="VirtualFolder"/> representing the directory tree, or null if decryption fails.</returns>
    Task<VirtualFolder?> LoadArchiveMetadataAsync(string archivePath);

    /// <summary>
    /// Decrypts and decompresses the entire contents of the currently loaded archive.
    /// </summary>
    /// <param name="destinationPath">The folder where the files will be extracted.</param>
    /// <param name="cancellationTokenSrc">Token to abort the extraction process.</param>
    /// <param name="progress">Reporter for extraction progress updates.</param>
    Task UpackFullArchiveAsync(
        string destinationPath,
        CancellationTokenSource? cancellationTokenSrc = null,
        IProgress<ProgressReport>? progress = null);

    /// <summary>
    /// Decrypts and extracts only specific files or folders from the archive.
    /// </summary>
    /// <param name="paths">The internal virtual paths (from the VirtualCatalog) to be extracted.</param>
    /// <param name="destinationPath">The local folder where the selected items will be saved.</param>
    /// <param name="cancellationTokenSrc">Token to abort the selective extraction.</param>
    /// <param name="progress">Reporter for extraction progress updates.</param>
    Task UpackArchiveSelectiveAsync(
        IEnumerable<string> paths,
        string destinationPath,
        CancellationTokenSource? cancellationTokenSrc = null,
        IProgress<ProgressReport>? progress = null);

    /// <summary>
    /// Clears the currently loaded metadata and associated file handles from memory.
    /// Used when closing an archive or switching between files.
    /// </summary>
    Task ClearMetadata();
}