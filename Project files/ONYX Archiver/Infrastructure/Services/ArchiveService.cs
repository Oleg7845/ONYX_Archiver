using CryptoCore.Abstractions;
using CryptoCore.Cryptography.Encryption.XChaCha20;
using CryptoCore.Cryptography.Hashing;
using CryptoCore.Cryptography.Keys.Ed25519;
using CryptoCore.Cryptography.Keys.X25519;
using OnyxArchiver.Core.IO;
using OnyxArchiver.Core.Models.Archive;
using OnyxArchiver.Core.Models.Archive.BlockIndex;
using OnyxArchiver.Core.Models.Archive.FileIndex;
using OnyxArchiver.Core.Models.Archive.VirtualCatalog;
using OnyxArchiver.Domain.Entities;
using OnyxArchiver.Domain.Exceptions;
using OnyxArchiver.Domain.Interfaces;
using System.IO;
using System.Security.Cryptography;

namespace OnyxArchiver.Infrastructure.Services;

/// <summary>
/// High-level service that orchestrates the archiving and unarchiving workflows.
/// Bridges the gap between domain logic (Packer/Unpacker) and security infrastructure.
/// </summary>
public class ArchiveService : IArchiveService
{
    private readonly IAuthService _authService;
    private readonly IPeerRepository _peerRepository;
    private readonly IUserVaultService _userVaultService;

    // --- Cached Archive Metadata State ---
    private ArchiveHeader? Header { get; set; }
    private VirtualFolder? VirtualFolder { get; set; }
    private FileIndex? FileIndex { get; set; }
    private BlockIndex? BlockIndex { get; set; }
    private string ArchivePath { get; set; } = string.Empty;

    public ArchiveService(
        IAuthService authService,
        IPeerRepository peerRepository,
        IUserVaultService userVaultService)
    {
        _authService = authService;
        _peerRepository = peerRepository;
        _userVaultService = userVaultService;
    }

    /// <summary>
    /// Creates a new encrypted archive tailored for a specific recipient (peer).
    /// Derives unique session keys using Ephemeral-Static X25519 Diffie-Hellman exchange.
    /// </summary>
    public async Task CreateArchiveAsync(
        int peerId,
        string sourcePath,
        string destinationPath,
        string archiveName,
        CancellationTokenSource? cancellationTokenSrc = null,
        IProgress<ProgressReport>? progress = null)
    {
        byte[]? sharedSecret = null;
        byte[]? symmetricKey = null;

        try
        {
            // 1. Resolve recipient from local storage
            PeerEntity? peer = await _peerRepository.GetByPeerByIdAsync(peerId);
            if (peer == null) throw new PeerNotFoundException();

            var archiveId = Guid.NewGuid();
            byte[] salt = Hkdf.GenerateRandomSalt();

            // 2. Setup crypto context using the user's master vault keys
            using IDecrypter decrypter = await _userVaultService.GetDecrypter(peer.Salt, peer.PublicKey);

            // 3. Prepare the Ed25519 signer for archive integrity verification
            using var peerEd25519KeyContext = Ed25519KeyContext.ImportEncrypted(peer.LocalSignaturePrivateKey, decrypter!);
            using var signer = new Ed25519Provider(peerEd25519KeyContext);

            // 4. Execute Diffie-Hellman Key Exchange
            using var ephemeralX25519KeyContext = new X25519KeyContext();
            byte[] recipientEncryptionPublicKey = decrypter.Decrypt(peer.RecipientEncryptionPublicKey);

            // Derive a shared secret between our ephemeral private key and peer's static public key
            sharedSecret = ephemeralX25519KeyContext.DeriveSharedSecret(recipientEncryptionPublicKey);

            // 5. Derive the final XChaCha20 symmetric key via HKDF
            symmetricKey = Hkdf.DeriveKey(
                inputKeyingMaterial: sharedSecret,
                salt: salt,
                info: archiveId.ToByteArray()).Key;

            using var encrypter = new XChaCha20Poly1305Provider(
                key: symmetricKey,
                associatedData: archiveId.ToByteArray());

            CancellationToken cancellationToken = cancellationTokenSrc?.Token ?? CancellationToken.None;

            // 6. Execute the packing process in a background thread to keep UI responsive
            await Task.Run(async () => {
                using (var packer = new Packer(
                    archiveId,
                    sourcePath,
                    destinationPath,
                    encrypter,
                    signer,
                    peer.PeerId,
                    salt,
                    publicKey: ephemeralX25519KeyContext.PublicKey,
                    keyIdentifier: await GetSaltedHash(sharedSecret, salt)))
                {
                    packer.Pack(archiveName, cancellationToken, progress);
                }
            });
        }
        finally
        {
            // Security: Securely wipe sensitive key material from managed memory
            if (sharedSecret != null) CryptographicOperations.ZeroMemory(sharedSecret);
            if (symmetricKey != null) CryptographicOperations.ZeroMemory(symmetricKey);
        }
    }

    /// <summary>
    /// Loads and validates archive metadata without extracting the payload.
    /// Performs peer identification based on the KeyIdentifier in the header.
    /// </summary>
    public async Task<VirtualFolder?> LoadArchiveMetadataAsync(string archivePath)
    {
        using (var unpacker = new Unpacker(archivePath))
        {
            ArchivePath = archivePath;
            Header = unpacker.UnpackHeader();

            // Locate the corresponding peer to reconstruct the decryption key
            PeerEntity? peer = await FindPeer(Header.Salt, Header.PublicKey, Header.KeyIdentifier);
            if (peer == null) throw new PeerNotFoundException();

            using IDecrypter peerDecrypter = await _userVaultService.GetDecrypter(peer.Salt, peer.PublicKey);
            using IDecrypter archiveDecrypter = await GetArchiveDecrypter();

            // Setup Ed25519 context for signature verification
            using var ed25519KeyContext = new Ed25519KeyContext();
            using IVerifier archiveVerifier = new Ed25519Provider(ed25519KeyContext);

            // Decrypt and verify the authenticity of File/Block indices
            unpacker.UnpackMetadata(
                decrypter: archiveDecrypter,
                verifier: archiveVerifier,
                signaturePublicKey: peerDecrypter.Decrypt(peer.RecipientSignaturePublicKey));

            VirtualFolder = unpacker.VirtualFolder;
            FileIndex = unpacker.FileIndex;
            BlockIndex = unpacker.BlockIndex;

            return VirtualFolder;
        }
    }

    /// <summary>
    /// Extracts all files from the current archive to the specified destination.
    /// </summary>
    public async Task UpackFullArchiveAsync(
        string destinationPath,
        CancellationTokenSource? cancellationTokenSrc = null,
        IProgress<ProgressReport>? progress = null)
    {
        CancellationToken cancellationToken = cancellationTokenSrc?.Token ?? CancellationToken.None;
        using IDecrypter archiveDecrypter = await GetArchiveDecrypter();

        await Task.Run(() => {
            if (Header == null || VirtualFolder == null || FileIndex == null || BlockIndex == null)
                throw new InvalidOperationException("Archive metadata must be loaded before unpacking.");

            using (var unpacker = new Unpacker(
                archiveHeader: Header,
                virtualFolder: VirtualFolder,
                fileIndex: FileIndex,
                blockIndex: BlockIndex,
                decrypter: archiveDecrypter,
                archivePath: ArchivePath))
            {
                unpacker.UnpackAllFiles(destinationPath, cancellationToken, progress);
            }
        });
    }

    /// <summary>
    /// Extracts specific files or directories based on their virtual paths.
    /// Handles directory structure restoration for nested items.
    /// </summary>
    public async Task UpackArchiveSelectiveAsync(
        IEnumerable<string> paths,
        string destinationPath,
        CancellationTokenSource? cancellationTokenSrc = null,
        IProgress<ProgressReport>? progress = null)
    {
        CancellationToken cancellationToken = cancellationTokenSrc?.Token ?? CancellationToken.None;
        using IDecrypter archiveDecrypter = await GetArchiveDecrypter();

        await Task.Run(() => {
            if (Header == null || VirtualFolder == null || FileIndex == null || BlockIndex == null)
                throw new InvalidOperationException("Metadata is required for selective unpacking.");

            using (var unpacker = new Unpacker(
                archiveHeader: Header,
                virtualFolder: VirtualFolder,
                fileIndex: FileIndex,
                blockIndex: BlockIndex,
                decrypter: archiveDecrypter,
                archivePath: ArchivePath))
            {
                foreach (string path in paths)
                {
                    if (VirtualFolder.IsSubfolderByPath(path))
                    {
                        // Directory processing: flatten relative structure for extraction
                        List<VirtualFile> files = VirtualFolder.FindFilesByPath(path);
                        foreach (VirtualFile file in files)
                        {
                            string filePath = file.Path;
                            if (!string.IsNullOrEmpty(Path.GetDirectoryName(path)))
                                filePath = Path.GetRelativePath(Path.GetDirectoryName(path)!, filePath);

                            string fullFolderPath = Path.Combine(destinationPath, Path.GetDirectoryName(filePath)!)
                                .Replace('\\', '/');

                            Directory.CreateDirectory(fullFolderPath);
                            unpacker.UnpackFile(file.Id, fullFolderPath, cancellationToken, progress);
                        }
                    }
                    else
                    {
                        // Single file processing
                        VirtualFile? file = VirtualFolder.FindFilesByPath(path).FirstOrDefault();
                        if (file == null) throw new FileNotFoundException($"Virtual file not found: {path}");

                        unpacker.UnpackFile(file.Id, destinationPath, cancellationToken, progress);
                    }
                }
            }
        });
    }

    /// <summary>
    /// Reconstructs the symmetric XChaCha20 key using the recipient's private key.
    /// </summary>
    private async Task<IDecrypter> GetArchiveDecrypter()
    {
        byte[]? sharedSecret = null;
        byte[]? symmetricKey = null;

        try
        {
            PeerEntity? peer = await FindPeer(Header!.Salt, Header.PublicKey, Header.KeyIdentifier);
            if (peer == null) throw new PeerNotFoundException();

            using IDecrypter peerDecrypter = await _userVaultService.GetDecrypter(peer.Salt, peer.PublicKey);

            // Import peer's encrypted private key and perform DH exchange to recover shared secret
            sharedSecret = X25519KeyContext.ImportEncrypted(peer.LocalEncryptionPrivateKey, peerDecrypter)
                .DeriveSharedSecret(Header.PublicKey);

            symmetricKey = Hkdf.DeriveKey(
                inputKeyingMaterial: sharedSecret,
                salt: Header.Salt,
                info: Header.ArchiveId.ToByteArray()).Key;

            return new XChaCha20Poly1305Provider(key: symmetricKey, associatedData: Header.ArchiveId.ToByteArray());
        }
        finally
        {
            if (sharedSecret != null) CryptographicOperations.ZeroMemory(sharedSecret);
            if (symmetricKey != null) CryptographicOperations.ZeroMemory(symmetricKey);
        }
    }

    public async Task ClearMetadata()
    {
        Header = null;
        VirtualFolder = null;
        FileIndex = null;
        BlockIndex = null;
        ArchivePath = string.Empty;
    }

    /// <summary>
    /// Computes a SHA-512 identifier for the key exchange.
    /// This allows peer lookup without exposing the static public key in plain text.
    /// </summary>
    private async Task<byte[]> GetSaltedHash(byte[] sharedSecret, byte[] salt)
    {
        using var sha512 = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
        sha512.AppendData(sharedSecret);
        sha512.AppendData(salt);
        return sha512.GetHashAndReset();
    }

    /// <summary>
    /// Identifies the correct recipient peer by attempting to match the KeyIdentifier hash.
    /// </summary>
    private async Task<PeerEntity?> FindPeer(byte[] salt, byte[] publicKey, byte[] keyIdentifier)
    {
        List<PeerEntity>? peers = await _peerRepository.GetAllPeersAsync(_authService.Username);
        if (peers == null) return null;

        foreach (var peer in peers)
        {
            using IDecrypter decrypter = await _userVaultService.GetDecrypter(peer.Salt, peer.PublicKey);
            using var peerX25519KeyContext = X25519KeyContext.ImportEncrypted(peer.LocalEncryptionPrivateKey, decrypter!);

            byte[] hash = await GetSaltedHash(peerX25519KeyContext.DeriveSharedSecret(publicKey), salt);
            if (keyIdentifier.AsSpan().SequenceEqual(hash)) return peer;
        }

        return null;
    }
}