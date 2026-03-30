using OnyxArchiver.UI.Models;

namespace OnyxArchiver.Domain.Interfaces;

/// <summary>
/// Orchestrates the lifecycle of peer connections and the multi-step cryptographic handshake process.
/// Handles everything from initial identity exchange to long-term contact management.
/// </summary>
public interface IPeerService
{
    /// <summary>
    /// Retrieves all peer contacts for the current user session.
    /// </summary>
    /// <returns>A collection of Data Transfer Objects representing established peers.</returns>
    Task<List<PeerDTO>> GetAllPeersAsync();

    /// <summary>
    /// Retrieves a paginated subset of peers to support efficient UI rendering in large contact lists.
    /// </summary>
    /// <param name="offset">The number of peer records to skip.</param>
    /// <param name="limit">The maximum number of peer records to return.</param>
    Task<List<PeerDTO>> GetPeersBatchAsync(int offset, int limit);

    /// <summary>
    /// Step 1 (Initiator): Generates an ephemeral key pair and creates an export file (.onyx)
    /// to be sent to a potential contact to begin the trust establishment.
    /// </summary>
    /// <param name="peerName">The local display name assigned to this potential peer.</param>
    /// <param name="handshakeFilePath">The target filesystem path for the generated handshake file.</param>
    /// <exception cref="PeerCreatingException">Thrown if the identity generation or file I/O fails.</exception>
    Task CreateKeyExchangeFileAsync(string peerName, string handshakeFilePath);

    /// <summary>
    /// Step 2 (Recipient): Processes an incoming handshake file, verifies the sender's 
    /// signature, and generates a response file containing the recipient's public keys.
    /// </summary>
    /// <param name="peerName">The display name to associate with this new contact.</param>
    /// <param name="handshakeFilePath">Path to the received .onyx handshake file.</param>
    /// <param name="handshakeOutDirectoryPath">Directory where the response handshake file will be created.</param>
    /// <exception cref="ReadHandshakeFileException">Thrown if the incoming file is malformed or invalid.</exception>
    Task AcceptHandshakeAsync(string peerName, string handshakeFilePath, string handshakeOutDirectoryPath);

    /// <summary>
    /// Step 3 (Initiator Finalization): Processes the response from a peer to confirm 
    /// the shared secret and finalize the secure connection in the database.
    /// </summary>
    /// <param name="handshakeFilePath">Path to the final handshake confirmation file received from the peer.</param>
    /// <exception cref="PeerUpdatingException">Thrown if the final key material cannot be saved to the vault.</exception>
    Task FinalizeHandshakeAsync(string handshakeFilePath);

    /// <summary>
    /// Updates the metadata (e.g., friendly display name) of an existing peer contact.
    /// </summary>
    /// <param name="peerDTO">The DTO containing the updated peer information.</param>
    Task UpdatePeerNameAsync(PeerDTO peerDTO);

    /// <summary>
    /// Permanently removes a peer and their associated cryptographic public keys from the system.
    /// </summary>
    /// <param name="id">The unique database identifier of the peer.</param>
    /// <exception cref="PeerDeletingException">Thrown if the peer record or associated keys cannot be removed.</exception>
    Task DeletePeerAsync(int id);
}