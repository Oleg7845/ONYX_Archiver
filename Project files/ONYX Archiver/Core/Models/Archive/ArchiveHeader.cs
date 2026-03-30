using System.IO;
using System.Text;

namespace OnyxArchiver.Core.Models.Archive;

/// <summary>
/// Represents the immutable structure at the beginning of an Onyx archive file.
/// Provides the fundamental metadata, cryptographic anchors, and pointers to internal indices.
/// </summary>
/// <remarks>
/// The header is always a fixed size (<see cref="FixedSize"/>), allowing for immediate 
/// parsing regardless of the archive's total volume.
/// </remarks>
public class ArchiveHeader
{
    /// <summary> Unique file signature: "ONX". </summary>
    public const string Magic = "ONX";

    /// <summary> Structural version of the archive format. </summary>
    public const ushort Version = 1;

    /// <summary> Unique identifier for this specific archive instance. </summary>
    public Guid ArchiveId { get; set; }

    /// <summary> The 32-byte salt used for the key derivation of this archive. </summary>
    public byte[] Salt { get; set; }

    /// <summary> The 32-byte public key (X25519) used for session key agreement. </summary>
    public byte[] PublicKey { get; set; }

    /// <summary> A 64-byte identifier (often a hash or signature) to verify key ownership. </summary>
    public byte[] KeyIdentifier { get; set; }

    #region Index Pointers
    // Catalog: General metadata about the archive structure
    public long CatalogOffset { get; set; }
    public long CatalogLength { get; set; }     // Compressed/Encrypted size
    public long CatalogRawLength { get; set; }  // Decompressed/Plaintext size

    // FileIndex: Map of all files and folders in the archive
    public long FileIndexOffset { get; set; }
    public long FileIndexLength { get; set; }
    public long FileIndexRawLength { get; set; }

    // BlockIndex: Low-level map of data chunks for deduplication/integrity
    public long BlockIndexOffset { get; set; }
    public long BlockIndexLength { get; set; }
    public long BlockIndexRawLength { get; set; }
    #endregion

    public const int MagicSize = 4;
    public const int VersionSize = 2;
    public const int ArchiveIdSize = 16;
    public const int SaltSize = 32;
    public const int PublicKeySize = 32;
    public const int KeyIdentifierSize = 64;
    public const int CatalogOffsetSize = 8;
    public const int CatalogLengthSize = 8;
    public const int CatalogRawLengthSize = 8;
    public const int FileIndexOffsetSize = 8;
    public const int FileIndexLengthSize = 8;
    public const int FileIndexRawLengthSize = 8;
    public const int BlockIndexOffsetSize = 8;
    public const int BlockIndexLengthSize = 8;
    public const int BlockIndexRawLengthSize = 8;
    public const int SignatureSize = 64;

    /// <summary> Total fixed size of the header in bytes (222 bytes). </summary>
    public const ushort FixedSize =  // 222 bytes
        MagicSize +
        VersionSize +
        ArchiveIdSize +
        SaltSize +
        PublicKeySize +
        KeyIdentifierSize +
        CatalogOffsetSize +
        CatalogLengthSize +
        CatalogRawLengthSize +
        FileIndexOffsetSize +
        FileIndexLengthSize +
        FileIndexRawLengthSize +
        BlockIndexOffsetSize +
        BlockIndexLengthSize +
        BlockIndexRawLengthSize;

    /// <summary>
    /// Initializes a new instance of <see cref="ArchiveHeader"/> with comprehensive validation.
    /// </summary>
    public ArchiveHeader(
        string magic,
        ushort version,
        Guid archiveId,
        byte[] salt,
        byte[] publicKey,
        byte[] keyIdentifier,
        long catalogOffset,
        long catalogLength,
        long catalogRawLength,
        long fileIndexOffset,
        long fileIndexLength,
        long fileIndexRawLength,
        long blockIndexOffset,
        long blockIndexLength,
        long blockIndexRawLength)
    {
        if (string.IsNullOrWhiteSpace(magic) || Encoding.UTF8.GetByteCount(magic) != Magic.Length)
            throw new ArgumentException($"Magic must be exactly {MagicSize} UTF8 bytes.");

        if (magic != Magic)
            throw new ArgumentException($"Magic must be \"{Magic}\"");

        if (version != Version)
            throw new ArgumentException($"Version must be \"{Version}\"");

        if (salt.Length != SaltSize)
            throw new ArgumentException($"Salt must be {SaltSize} bytes.");

        if (publicKey.Length != PublicKeySize)
            throw new ArgumentException($"Public key must be {PublicKeySize} bytes.");

        if (keyIdentifier.Length != KeyIdentifierSize)
            throw new ArgumentException($"Key identifier must be {KeyIdentifierSize} bytes.");

        ArchiveId = archiveId;
        Salt = salt;
        PublicKey = publicKey;
        KeyIdentifier = keyIdentifier;
        CatalogOffset = catalogOffset;
        CatalogLength = catalogLength;
        CatalogRawLength = catalogRawLength;
        FileIndexOffset = fileIndexOffset;
        FileIndexLength = fileIndexLength;
        FileIndexRawLength = fileIndexRawLength;
        BlockIndexOffset = blockIndexOffset;
        BlockIndexLength = blockIndexLength;
        BlockIndexRawLength = blockIndexRawLength;
    }

    public ArchiveHeader(
        Guid archiveId,
        byte[] salt,
        byte[] publicKey,
        byte[] keyIdentifier,
        long catalogOffset,
        long catalogLength,
        long catalogRawLength,
        long fileIndexOffset,
        long fileIndexLength,
        long fileIndexRawLength,
        long blockIndexOffset,
        long blockIndexLength,
        long blockIndexRawLength) : this(
            Magic,
            Version,
            archiveId,
            salt,
            publicKey,
            keyIdentifier,
            catalogOffset,
            catalogLength,
            catalogRawLength,
            fileIndexOffset,
            fileIndexLength,
            fileIndexRawLength,
            blockIndexOffset,
            blockIndexLength,
            blockIndexRawLength)
    { }

    /// <summary>
    /// Serializes the header into a byte array for writing to the file disk.
    /// Uses a fixed-length buffer to ensure binary consistency.
    /// </summary>
    /// <returns>A byte array of <see cref="FixedSize"/>.</returns>
    public byte[] Serialize()
    {
        var buffer = new byte[FixedSize];
        using MemoryStream ms = new MemoryStream(buffer);
        using BinaryWriter bw = new BinaryWriter(ms, Encoding.UTF8);

        // Write the Magic
        byte[] magicBytes = new byte[MagicSize];
        Encoding.UTF8.GetBytes(Magic).CopyTo(magicBytes, 0);
        bw.Write(magicBytes);

        // Write other
        bw.Write(Version);
        bw.Write(ArchiveId.ToByteArray());
        bw.Write(Salt);
        bw.Write(PublicKey);
        bw.Write(KeyIdentifier);
        bw.Write(CatalogOffset);
        bw.Write(CatalogLength);
        bw.Write(CatalogRawLength);
        bw.Write(FileIndexOffset);
        bw.Write(FileIndexLength);
        bw.Write(FileIndexRawLength);
        bw.Write(BlockIndexOffset);
        bw.Write(BlockIndexLength);
        bw.Write(BlockIndexRawLength);

        return buffer;
    }

    /// <summary>
    /// Reads a byte array and reconstructs the <see cref="ArchiveHeader"/>.
    /// Performs sanity checks on Magic and Version to prevent opening incompatible files.
    /// </summary>
    /// <param name="data">The first 222 bytes of the file.</param>
    /// <returns>A validated header instance.</returns>
    /// <exception cref="InvalidDataException">Thrown if the magic signature is missing.</exception>
    public static ArchiveHeader Deserialize(byte[] data)
    {
        if (data == null || data.Length != FixedSize)
            throw new ArgumentException($"Data must be exactly {FixedSize} bytes.");

        using MemoryStream ms = new MemoryStream(data);
        using BinaryReader br = new BinaryReader(ms, Encoding.UTF8);

        var magic = Encoding.UTF8.GetString(br.ReadBytes(MagicSize)).Trim('\0');

        if (magic != Magic)
            throw new InvalidDataException("Invalid magic.");

        return new ArchiveHeader(
            magic: magic,
            version: br.ReadUInt16(),
            archiveId: new Guid(br.ReadBytes(ArchiveIdSize)),
            salt: br.ReadBytes(SaltSize),
            publicKey: br.ReadBytes(PublicKeySize),
            keyIdentifier: br.ReadBytes(KeyIdentifierSize),
            catalogOffset: br.ReadInt64(),
            catalogLength: br.ReadInt64(),
            catalogRawLength: br.ReadInt64(),
            fileIndexOffset: br.ReadInt64(),
            fileIndexLength: br.ReadInt64(),
            fileIndexRawLength: br.ReadInt64(),
            blockIndexOffset: br.ReadInt64(),
            blockIndexLength: br.ReadInt64(),
            blockIndexRawLength: br.ReadInt64());
    }
}
