namespace OnyxArchiver.UI.Messages;

/// <summary>
/// A message record used to broadcast a file system path during the Handshake process.
/// Typically used to notify the UI or a background service about the location 
/// of exchange files (e.g., public keys or session initiation files).
/// </summary>
/// <param name="Path">The full string path involved in the cryptographic handshake.</param>
public record HandshakePathMessage(string Path);