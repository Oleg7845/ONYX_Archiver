using OnyxArchiver.UI.Models;

namespace OnyxArchiver.UI.Messages;

/// <summary>
/// A lightweight message record used to broadcast the selection of a specific peer.
/// Typically used within a Mediator pattern (e.g., MVVM Toolkit Messenger) 
/// to notify other ViewModels that a user context has changed.
/// </summary>
/// <param name="Peer">The Data Transfer Object representing the currently selected peer/contact.</param>
public record SelectedPeerMessage(PeerDTO Peer);