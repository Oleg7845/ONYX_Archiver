using OnyxArchiver.UI.Models;

namespace OnyxArchiver.UI.Messages;

/// <summary>
/// A messaging record dispatched when a new peer should be instantiated 
/// or added to the active peer collection.
/// </summary>
/// <param name="Peer">The Data Transfer Object containing the initial information of the new peer.</param>
public record CreatePeerMessage(PeerDTO Peer);
