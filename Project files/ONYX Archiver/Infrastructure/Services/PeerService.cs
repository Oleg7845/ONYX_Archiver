using CommunityToolkit.Mvvm.Messaging;
using CryptoCore.Abstractions;
using OnyxArchiver.Core.KeyExchange;
using OnyxArchiver.Core.Models.Cryptography;
using OnyxArchiver.Core.Models.KeyExcahnge;
using OnyxArchiver.Domain.Entities;
using OnyxArchiver.Domain.Exceptions;
using OnyxArchiver.Domain.Interfaces;
using OnyxArchiver.UI.Messages;
using OnyxArchiver.UI.Models;
using System.Diagnostics;
using System.IO;

namespace OnyxArchiver.Infrastructure.Services;

/// <summary>
/// Manages the lifecycle of peer connections and the cryptographic handshake process.
/// Orchestrates the exchange of X25519 and Ed25519 public keys between users.
/// </summary>
public class PeerService : IPeerService
{
    private readonly IPeerRepository _peerRepository;
    private readonly IAuthService _authService;
    private readonly IUserVaultService _userVaultService;

    public PeerService(
        IPeerRepository peerRepository,
        IAuthService authService,
        IUserVaultService userVaultService)
    {
        _peerRepository = peerRepository;
        _userVaultService = userVaultService;
        _authService = authService;
    }

    /// <summary>
    /// Retrieves all registered peers for the currently authenticated user.
    /// </summary>
    /// <returns>A list of <see cref="PeerDTO"/> objects containing display information for each contact.</returns>
    public async Task<List<PeerDTO>> GetAllPeersAsync()
    {
        var peersList = new List<PeerDTO>();

        // Fetch all peer entities associated with the current session's username
        List<PeerEntity>? entities = await _peerRepository.GetAllPeersAsync(_authService.Username);

        if (entities == null || !entities.Any()) return peersList;

        foreach (var entity in entities)
        {
            peersList.Add(await MapToPeerDTOAsync(entity));
        }

        return peersList;
    }

    /// <summary>
    /// Retrieves a specific subset of peers using offset-based pagination.
    /// </summary>
    public async Task<List<PeerDTO>> GetPeersBatchAsync(int offset, int limit)
    {
        var peersList = new List<PeerDTO>();

        // Optimized retrieval for large contact lists to support UI virtualization
        List<PeerEntity>? entities = await _peerRepository.GetPeersBatchAsync(_authService.Username, offset, limit);

        if (entities == null || !entities.Any()) return peersList;

        foreach (var entity in entities)
        {
            peersList.Add(await MapToPeerDTOAsync(entity));
        }

        return peersList;
    }

    /// <summary>
    /// Initiates a new handshake. Generates local keys and creates a (.onyx) file 
    /// to be sent to the recipient.
    /// </summary>
    public async Task CreateKeyExchangeFileAsync(string peerName, string handshakeDirectoryPath)
    {
        // Access protected crypto context from the user's secure vault
        using EncryptionContext encryptionContext = await _userVaultService.GetEncryptionContext();

        // Generate the initial handshake payload containing ephemeral public keys
        KeyExchangeBundle kexBundle = KeyExchangeManager.CreateHandshake(encryptionContext.Encrypter);

        var peerEntity = new PeerEntity
        {
            Username = _authService.Username,
            PeerId = kexBundle.Context.Id,
            Name = peerName,
            Salt = encryptionContext.Salt,
            PublicKey = encryptionContext.PublicKey,
            LocalEncryptionPrivateKey = kexBundle.PrivateEncryptionKey,
            LocalSignaturePrivateKey = kexBundle.PrivateSignatureKey
        };

        try
        {
            // Ensure target directory exists for exporting the .onyx handshake file
            if (!Directory.Exists(handshakeDirectoryPath))
                Directory.CreateDirectory(handshakeDirectoryPath);

            string fullPath = Path.Combine(
                handshakeDirectoryPath,
                $"handshake-{peerEntity.PeerId}-begin.{KeyExchangeContext.Magic.ToLower()}");

            // Persist the handshake payload to disk for manual transport to the peer
            using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            {
                await fs.WriteAsync(kexBundle.Context.Serialize(), 0, KeyExchangeContext.FixedSize);
            }

            // Record the pending peer relationship in the local database
            if (!await _peerRepository.AddAsync(peerEntity))
            {
                // Rollback: delete the file if database persistence fails
                if (File.Exists(fullPath))
                    File.Delete(fullPath);

                throw new PeerCreatingException();
            }

            // Notify UI components about the new peer addition
            WeakReferenceMessenger.Default.Send(new CreatePeerMessage(
                   await MapToPeerDTOAsync(peerEntity)));
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            Debug.WriteLine($"Critical error during peer creation: {ex.Message}");
            throw new DomainException("Peer creation failed due to an infrastructure error", ex.Message);
        }
    }

    /// <summary>
    /// Updates the display name of an existing peer in the local database.
    /// </summary>
    public async Task UpdatePeerNameAsync(PeerDTO peerDTO)
    {
        try
        {
            PeerEntity? entity = await _peerRepository.GetByPeerByIdAsync(peerDTO.Id);

            if (entity == null)
                throw new PeerNotFoundException();

            entity.Name = peerDTO.Name;

            if (!await _peerRepository.UpdateAsync(entity))
                throw new PeerUpdatingException(entity.PeerId);
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            Debug.WriteLine($"Critical error during peer update: {ex.Message}");
            throw new DomainException("Failed to update peer information", ex.Message);
        }
    }

    /// <summary>
    /// Permanently removes a peer and their associated keys from the local database.
    /// </summary>
    public async Task DeletePeerAsync(int id)
    {
        try
        {
            // Revoke trust and purge keys associated with this specific peer ID
            if (!await _peerRepository.DeleteAsync(id))
                throw new PeerDeletingException();
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            Debug.WriteLine($"Critical error during peer deletion: {ex.Message}");
            throw new DomainException("An unexpected error occurred while deleting the peer", ex.Message);
        }
    }

    /// <summary>
    /// Processes an incoming handshake file (Step 2). Generates a response file 
    /// and stores the peer's public keys.
    /// </summary>
    public async Task AcceptHandshakeAsync(string peerName, string inputHandshakeFilePath, string handshakeOutDirectoryPath)
    {
        try
        {
            // Validate and read the peer's initial handshake package
            byte[] inputHandshakeFileBytes = await ReadHandshakeFileAsync(inputHandshakeFilePath);

            using EncryptionContext encryptionContext = await _userVaultService.GetEncryptionContext();

            // Validate peer signature and derive the shared response payload
            KeyExchangeBundle kexBundle = KeyExchangeManager.AcceptHandshake(inputHandshakeFileBytes!, encryptionContext.Encrypter);

            var peerEntity = new PeerEntity
            {
                Username = _authService.Username,
                PeerId = kexBundle.Context.Id,
                Name = peerName,
                Salt = encryptionContext.Salt,
                PublicKey = encryptionContext.PublicKey,
                LocalEncryptionPrivateKey = kexBundle.PrivateEncryptionKey,
                LocalSignaturePrivateKey = kexBundle.PrivateSignatureKey,
                RecipientEncryptionPublicKey = kexBundle.PublicEncryptionKey,
                RecipientSignaturePublicKey = kexBundle.PublicSignatureKey,
                FinalizedAt = DateTime.UtcNow // Connection is established on recipient's end
            };

            if (!Directory.Exists(handshakeOutDirectoryPath))
                Directory.CreateDirectory(handshakeOutDirectoryPath);

            string fullPath = Path.Combine(
                handshakeOutDirectoryPath,
                $"handshake-{peerEntity.PeerId}-end.{KeyExchangeContext.Magic.ToLower()}");

            // Write the completion payload to be sent back to the initiator
            using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            {
                await fs.WriteAsync(kexBundle.Context.Serialize(), 0, KeyExchangeContext.FixedSize);
            }

            if (!await _peerRepository.AddAsync(peerEntity))
            {
                if (File.Exists(fullPath))
                    File.Delete(fullPath);

                throw new PeerCreatingException();
            }

            WeakReferenceMessenger.Default.Send(new CreatePeerMessage(
                    await MapToPeerDTOAsync(peerEntity)));
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            Debug.WriteLine($"Critical error during handshake acceptance: {ex.Message}");
            throw new DomainException("Could not accept the handshake from the provided file", ex.Message);
        }
    }

    /// <summary>
    /// Finalizes the handshake process (Step 3) for the initiator.
    /// </summary>
    public async Task FinalizeHandshakeAsync(string inputHandshakeFilePath)
    {
        try
        {
            byte[] inputHandshakeFileBytes = await ReadHandshakeFileAsync(inputHandshakeFilePath);

            // Extract the recipient's finalized public keys from the response
            using KeyExchangeContext kexContext = KeyExchangeManager.FinalizeHandshake(inputHandshakeFileBytes!);

            PeerEntity? peerEntity = await _peerRepository.GetByPeerByPeerIdAsync(kexContext.Id);

            if (peerEntity == null)
                throw new PeerNotFoundException(kexContext.Id);

            // Use the user vault to encrypt the recipient's public keys before database storage
            using IEncrypter encrypter = await _userVaultService.GetEncrypter(peerEntity.Salt, peerEntity.PublicKey);

            peerEntity.RecipientEncryptionPublicKey = encrypter.Encrypt(kexContext.EncryptionPublicKey);
            peerEntity.RecipientSignaturePublicKey = encrypter.Encrypt(kexContext.SignaturePublicKey);
            peerEntity.FinalizedAt = DateTime.UtcNow;

            if (!await _peerRepository.UpdateAsync(peerEntity))
                throw new PeerUpdatingException(peerEntity.PeerId);

            WeakReferenceMessenger.Default.Send(new UpdatePeerMessage(
                await MapToPeerDTOAsync(peerEntity)));
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            Debug.WriteLine($"Critical error during handshake finalization: {ex.Message}");
            throw new DomainException("Handshake completion failed", ex.Message);
        }
    }

    /// <summary>
    /// Sanitizes data for UI consumption by stripping sensitive key material.
    /// </summary>
    private async Task<PeerDTO> MapToPeerDTOAsync(PeerEntity entity)
    {
        return new PeerDTO
        {
            Id = entity.Id,
            PeerId = entity.PeerId,
            Name = entity.Name,
            CreatedAt = entity.CreatedAt,
            FinalizedAt = entity.FinalizedAt
        };
    }

    /// <summary>
    /// Reads and validates the structure of a handshake file.
    /// </summary>
    private async Task<byte[]> ReadHandshakeFileAsync(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);

            // Mandatory size check to prevent buffer manipulation or malformed file processing
            if (fileInfo.Length != KeyExchangeContext.FixedSize)
                throw new ReadHandshakeFileException("The selected file is corrupted or has an invalid format");

            using (var fs = new FileStream(
                path: filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true))
            {
                byte[] buffer = new byte[KeyExchangeContext.FixedSize];
                int bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead == buffer.Length)
                {
                    return buffer;
                }

                throw new ReadHandshakeFileException("Read incomplete: file content size mismatch");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"IO Error reading handshake file: {ex.Message}");
            throw new ReadHandshakeFileException();
        }
    }
}