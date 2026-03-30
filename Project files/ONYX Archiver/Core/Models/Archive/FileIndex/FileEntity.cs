using System.IO;

namespace OnyxArchiver.Core.Models.Archive.FileIndex;

/// <summary>
/// Represents the mapping between a logical file entry and its physical data distribution.
/// It acts as a container for all <see cref="FileSegment"/> objects that constitute a single file.
/// </summary>
/// <remarks>
/// By allowing multiple segments, this model supports fragmented storage and data deduplication, 
/// where different files can point to the same physical data blocks.
/// </remarks>
public class FileEntity
{
    /// <summary> 
    /// Gets the unique identifier of the file, matching the <see cref="VirtualCatalog.VirtualFile.Id"/>. 
    /// </summary>
    public Guid FileId { get; init; }

    /// <summary> 
    /// Gets or sets the list of segments that make up the file's data.
    /// Segments are stored in order to reconstruct the original file stream.
    /// </summary>
    public List<FileSegment> Segments { get; set; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="FileEntity"/> class.
    /// </summary>
    /// <param name="fileId">The unique ID of the associated virtual file.</param>
    public FileEntity(Guid fileId)
    {
        FileId = fileId;
    }

    /// <summary>
    /// Adds a data segment to the file's composition.
    /// </summary>
    /// <param name="segment">The segment mapping to a physical block.</param>
    public void Add(FileSegment segment)
    {
        Segments.Add(segment);
    }

    /// <summary>
    /// Serializes the file entity and all its segments into a binary stream.
    /// </summary>
    /// <param name="bw">The binary writer targeting the File Index section of the archive.</param>
    public void Serialize(BinaryWriter bw)
    {
        bw.Write(FileId.ToByteArray());

        // Write segments count for proper reconstruction during deserialization
        bw.Write(Segments.Count);
        foreach (var segment in Segments)
        {
            segment.Serialize(bw);
        }
    }

    /// <summary>
    /// Reconstructs a <see cref="FileEntity"/> from its binary representation.
    /// </summary>
    /// <param name="br">The binary reader positioned at the start of a file entity record.</param>
    /// <returns>A fully populated <see cref="FileEntity"/> instance.</returns>
    public static FileEntity Deserialize(BinaryReader br)
    {
        var entity = new FileEntity(
            new Guid(br.ReadBytes(16)));

        int segmentsCount = br.ReadInt32();
        for (int i = 0; i < segmentsCount; i++)
        {
            entity.Add(
                FileSegment.Deserialize(br));
        }

        return entity;
    }
}
