namespace OnyxArchiver.UI.Messages;

/// <summary>
/// A messaging record used to notify subscribers that a peer acceptance 
/// process has been initiated or confirmed.
/// Typically triggers UI updates or cryptographic key storage operations.
/// </summary>
/// <param name="PeerName">The unique display name or identifier of the peer being accepted.</param>
public record AcceptPeerMessage(string PeerName);