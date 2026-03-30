using System.IO;

namespace OnyxArchiver.Core.Models.Archive.BlockIndex;

/// <summary>
/// Represents a physical data block within the archive file.
/// Maps a logical segment of data to its absolute position and size on the disk.
/// </summary>
/// <remarks>
/// This entity is crucial for random access reading. It tracks both the stored (compressed/encrypted) 
/// length and the original raw length to facilitate precise memory allocation during decompression.
/// </remarks>
public class BlockEntity
{
    /// <summary> Gets the unique numerical identifier for this data block. </summary>
    public int BlockId { get; init; }

    /// <summary> Gets the absolute byte offset from the start of the archive file where this block begins. </summary>
    public long Offset { get; init; }

    /// <summary> Gets the actual size of the block as it is stored in the archive (after compression and encryption). </summary>
    public long Length { get; init; }

    /// <summary> Gets the original size of the data before any processing was applied. </summary>
    public long RawLength { get; init; }

    /// <summary> The fixed binary size of a serialized block entity (28 bytes). </summary>
    public int FixedSize = 4 + 8 + 8 + 8;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockEntity"/> class.
    /// </summary>
    /// <param name="blockId">Unique index of the block.</param>
    /// <param name="offset">Position in the stream.</param>
    /// <param name="length">Stored size (compressed/encrypted).</param>
    /// <param name="rawLength">Original size.</param>
    public BlockEntity(int blockId, long offset, long length, long rawLength)
    {
        BlockId = blockId;
        Offset = offset;
        Length = length;
        RawLength = rawLength;
    }

    /// <summary>
    /// Writes the block metadata to a binary stream.
    /// </summary>
    /// <param name="bw">The binary writer targeting the Block Index section.</param>
    public void Serialize(BinaryWriter bw)
    {
        bw.Write(BlockId);
        bw.Write(Offset);
        bw.Write(Length);
        bw.Write(RawLength);
    }

    /// <summary>
    /// Reads a block entry from the current position of the binary reader.
    /// </summary>
    /// <param name="br">The binary reader positioned at a block entry.</param>
    /// <returns>A populated <see cref="BlockEntity"/> instance.</returns>
    public static BlockEntity Deserialize(BinaryReader br)
    {
        return new BlockEntity(
            br.ReadInt32(),
            br.ReadInt64(),
            br.ReadInt64(),
            br.ReadInt64());
    }
}
