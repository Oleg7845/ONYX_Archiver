using OnyxArchiver.UI.Models;

namespace OnyxArchiver.UI.Messages;

/// <summary>
/// A messaging record dispatched when a peer's information has been modified.
/// Used to synchronize UI components like lists, headers, or detail views.
/// </summary>
/// <param name="UpdatedPeer">The Data Transfer Object containing the revised peer information.</param>
public record UpdatePeerMessage(PeerDTO UpdatedPeer);
