using System.IO;
using System.Text;

namespace OnyxArchiver.Core.Models.Archive.FileIndex;

/// <summary>
/// Acts as a central registry for mapping file identities to their physical data layouts.
/// Manages a collection of <see cref="FileEntity"/> objects, which define how each file 
/// is reconstructed from various data segments.
/// </summary>
/// <remarks>
/// The FileIndex is a critical part of the archive's metadata, enabling random access 
/// to file data without scanning the entire archive.
/// </remarks>
public class FileIndex
{
    /// <summary>
    /// Gets or sets the collection of file entities registered in this index.
    /// </summary>
    public List<FileEntity> Entities { get; set; } = [];

    /// <summary>
    /// Registers a new <see cref="FileEntity"/> in the index.
    /// </summary>
    /// <param name="entity">The file entity containing segment mappings.</param>
    public void Add(FileEntity entity)
    {
        Entities.Add(entity);
    }

    /// <summary>
    /// Retrieves a <see cref="FileEntity"/> by its unique identifier.
    /// </summary>
    /// <param name="id">The <see cref="Guid"/> matching the file's ID in the virtual catalog.</param>
    /// <returns>The found <see cref="FileEntity"/>, or <c>null</c> if no match exists.</returns>
    public FileEntity? GetEntityById(Guid id)
    {
        return Entities.Find(e => e.FileId == id);
    }

    /// <summary>
    /// Serializes the entire file index into a binary format for storage.
    /// </summary>
    /// <remarks>
    /// The structure starts with a 4-byte integer (Count), followed by 
    /// the serialized representation of each <see cref="FileEntity"/>.
    /// </remarks>
    /// <returns>A byte array representing the serialized file index.</returns>
    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms, Encoding.UTF8))
        {
            bw.Write(Entities.Count);
            foreach (var entity in Entities)
            {
                entity.Serialize(bw);
            }
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Reconstructs the <see cref="FileIndex"/> from a binary source.
    /// </summary>
    /// <param name="data">The byte array containing the serialized index.</param>
    /// <returns>A populated <see cref="FileIndex"/> instance.</returns>
    /// <exception cref="EndOfStreamException">Thrown if the binary data is truncated or corrupted.</exception>
    public static FileIndex Deserialize(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using (var br = new BinaryReader(ms, Encoding.UTF8))
        {
            var fileIndex = new FileIndex();

            int entitiesCount = br.ReadInt32();
            for (int i = 0; i < entitiesCount; i++)
            {
                fileIndex.Add(
                    FileEntity.Deserialize(br));
            }

            return fileIndex;
        }
    }
}
