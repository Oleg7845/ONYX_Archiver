using OnyxArchiver.UI.Models;

namespace OnyxArchiver.UI.Messages;

/// <summary>
/// A message record used for decoupled communication between ViewModels.
/// Carries configuration data for a specific archive operation.
/// </summary>
/// <param name="ArchiveName">The display name or filename of the archive.</param>
/// <param name="ArchivePath">The full file system path where the archive is located or will be created.</param>
/// <param name="SelectedPeer">The recipient or owner associated with this archive for cryptographic operations.</param>
public record ArchiveConfigurationMessage(
    string ArchiveName,
    string ArchivePath,
    PeerDTO SelectedPeer);