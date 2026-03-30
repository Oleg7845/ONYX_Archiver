using System.IO;
using System.Text;

namespace OnyxArchiver.Core.Models.Archive.BlockIndex;

/// <summary>
/// Acts as a central registry for all data blocks stored within the archive.
/// Manages the collection of <see cref="BlockEntity"/> records, facilitating 
/// the mapping between logical data requests and physical file offsets.
/// </summary>
public class BlockIndex
{
    /// <summary>
    /// A collection of all block metadata records in the archive.
    /// </summary>
    public List<BlockEntity> Records = [];

    /// <summary>
    /// Registers a new data block in the index.
    /// </summary>
    /// <param name="entity">The block metadata to add.</param>
    public void Add(BlockEntity entity)
    {
        Records.Add(entity);
    }

    /// <summary>
    /// Retrieves the metadata for a specific block using its unique ID.
    /// </summary>
    /// <param name="id">The unique identifier of the block.</param>
    /// <returns>The <see cref="BlockEntity"/> if found; otherwise, <c>null</c>.</returns>
    public BlockEntity? GetEntityById(int id)
    {
        return Records.Find(e => e.BlockId == id);
    }

    /// <summary>
    /// Serializes the entire block index into a binary format for storage within the archive file.
    /// </summary>
    /// <remarks>
    /// The format starts with a 4-byte integer representing the total number of records, 
    /// followed by the fixed-size serialized data for each <see cref="BlockEntity"/>.
    /// </remarks>
    /// <returns>A byte array representing the serialized block index.</returns>
    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms, Encoding.UTF8))
        {
            bw.Write(Records.Count);
            foreach (var record in Records)
            {
                record.Serialize(bw);
            }
        }
        return ms.ToArray();
    }

    /// <summary>
    /// Reconstructs the <see cref="BlockIndex"/> from a binary source.
    /// </summary>
    /// <param name="data">The byte array containing the serialized index from the archive.</param>
    /// <returns>A populated <see cref="BlockIndex"/> instance.</returns>
    public static BlockIndex Deserialize(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using (var br = new BinaryReader(ms, Encoding.UTF8))
        {
            var blockIndex = new BlockIndex();

            int recordsCount = br.ReadInt32();
            for (int i = 0; i < recordsCount; i++)
            {
                blockIndex.Records.Add(
                    BlockEntity.Deserialize(br));
            }

            return blockIndex;
        }
    }
}
