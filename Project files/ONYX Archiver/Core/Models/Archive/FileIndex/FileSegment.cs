using System.IO;

namespace OnyxArchiver.Core.Models.Archive.FileIndex;

/// <summary>
/// Represents a logical mapping of a file portion to a physical data block.
/// Acts as a bridge between the virtual file entry and the actual stored binary data.
/// </summary>
/// <remarks>
/// This structure supports data fragmentation and deduplication by allowing multiple files 
/// or file parts to reference the same or different physical blocks within the archive.
/// </remarks>
public class FileSegment
{
    /// <summary> Gets the unique identifier of the physical data block where this segment is stored. </summary>
    public int BlockId { get; init; }

    /// <summary> Gets the starting position of this segment's data within the referenced physical block. </summary>
    public long Offset { get; init; }

    /// <summary> Gets the length of the data segment in bytes. </summary>
    public long Length { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSegment"/> class.
    /// </summary>
    /// <param name="blockId">ID of the target physical block.</param>
    /// <param name="offset">Relative offset within the block.</param>
    /// <param name="length">Number of bytes to read from the block.</param>
    public FileSegment(int blockId, long offset, long length)
    {
        BlockId = blockId;
        Offset = offset;
        Length = length;
    }

    /// <summary>
    /// Writes the segment metadata to a binary stream.
    /// </summary>
    /// <param name="bw">The binary writer targeting the File Index section.</param>
    public void Serialize(BinaryWriter bw)
    {
        bw.Write(BlockId);
        bw.Write(Offset);
        bw.Write(Length);
    }

    /// <summary>
    /// Reconstructs a <see cref="FileSegment"/> from a binary source.
    /// </summary>
    /// <param name="br">The binary reader positioned at a segment record.</param>
    /// <returns>A populated <see cref="FileSegment"/> instance.</returns>
    public static FileSegment Deserialize(BinaryReader br)
    {
        return new FileSegment(
            br.ReadInt32(),
            br.ReadInt64(),
            br.ReadInt64());
    }
}
