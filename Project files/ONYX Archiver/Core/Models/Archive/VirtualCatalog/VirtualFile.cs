using System.IO;

namespace OnyxArchiver.Core.Models.Archive.VirtualCatalog;

/// <summary>
/// Represents a metadata entry for a file within the archive's virtual file system.
/// This class tracks the file's identity, path, and logical size independently of its physical storage.
/// </summary>
public class VirtualFile
{
    /// <summary> Unique identifier for the file entry. Useful for linking to physical data blocks. </summary>
    public Guid Id { get; set; }

    /// <summary> The full internal path of the file within the archive (e.g., "documents/report.pdf"). </summary>
    public string Path { get; set; }

    /// <summary> Gets the filename portion of the path. </summary>
    public string Name => System.IO.Path.GetFileName(Path);

    /// <summary> The original (uncompressed) size of the file in bytes. </summary>
    public long Size { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="VirtualFile"/>.
    /// Normalizes backslashes to forward slashes for cross-platform compatibility.
    /// </summary>
    public VirtualFile(Guid id, string path, long size)
    {
        Id = id;
        Path = path.Replace("\\", "/");
        Size = size;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="VirtualFile"/> with a generated <see cref="Guid"/>.
    /// </summary>
    public VirtualFile(string path, long size) : this(Guid.NewGuid(), path, size) { }

    /// <summary>
    /// Serializes the file metadata to a binary stream.
    /// </summary>
    /// <param name="bw">The binary writer targeting the index section of the archive.</param>
    public void Serialize(BinaryWriter bw)
    {
        bw.Write(Id.ToByteArray());
        bw.Write(Path);                 // BinaryWriter.Write(string) handles length-prefixing automatically
        bw.Write(Size);
    }

    /// <summary>
    /// Reconstructs a <see cref="VirtualFile"/> instance from a binary stream.
    /// </summary>
    /// <param name="br">The binary reader positioned at the file entry in the index.</param>
    /// <returns>A populated <see cref="VirtualFile"/> instance.</returns>
    public static VirtualFile Deserialize(BinaryReader br)
    {
        return new VirtualFile(
            new Guid(br.ReadBytes(16)),
            br.ReadString(),
            br.ReadInt64());
    }
}
