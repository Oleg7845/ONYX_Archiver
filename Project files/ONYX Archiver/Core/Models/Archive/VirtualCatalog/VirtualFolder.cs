using System.IO;
using System.Text;

namespace OnyxArchiver.Core.Models.Archive.VirtualCatalog;

/// <summary>
/// Represents a directory node within the archive's virtual file system.
/// Manages a hierarchical structure of subfolders and files using a recursive tree pattern.
/// </summary>
public class VirtualFolder
{
    /// <summary> 
    /// Gets or sets the internal path of the folder. 
    /// Paths are stored using forward slashes for cross-platform compatibility.
    /// </summary>
    public string Path { get; set; }

    /// <summary> 
    /// Gets the name of the folder extracted from its path. 
    /// </summary>
    public string Name => System.IO.Path.GetFileName(Path);

    /// <summary> 
    /// Collection of nested directories. 
    /// </summary>
    public List<VirtualFolder> Subfolders = [];

    /// <summary> 
    /// Collection of files contained directly within this folder level. 
    /// </summary>
    public List<VirtualFile> Files = [];

    /// <summary>
    /// Initializes a new instance of <see cref="VirtualFolder"/>.
    /// Standardizes path separators to forward slashes to avoid Windows/Linux path conflicts.
    /// </summary>
    public VirtualFolder(string path)
    {
        Path = path.Replace("\\", "/");
    }

    /// <summary>
    /// Converts all absolute paths within this folder and its children into relative paths.
    /// This is a critical step before serialization to ensure the archive can be 
    /// extracted on any machine regardless of the original source drive.
    /// </summary>
    public void SetRelativePaths()
    {
        if (Subfolders.Count > 0)
            SetRelativePathsRecursive(System.IO.Path.GetDirectoryName(Subfolders.FirstOrDefault()!.Path)!);
        else
            SetRelativePathsRecursive(Path);

        Path = System.IO.Path.GetFileName(Path);
    }

    /// <summary>
    /// Recursively updates the paths of this folder, all nested files, and subfolders 
    /// to be relative to a specified root directory using a depth-first traversal.
    /// </summary>
    public void SetRelativePathsRecursive(string rootPath)
    {
        if (System.IO.Path.GetFileName(rootPath) != Name)
            Path = System.IO.Path.GetRelativePath(rootPath, Path);

        foreach (var file in Files)
        {
            file.Path = System.IO.Path.GetRelativePath(rootPath, file.Path);
        }

        foreach (var subfolder in Subfolders)
        {
            subfolder.SetRelativePathsRecursive(rootPath);
        }
    }

    /// <summary>
    /// Flattens the folder hierarchy and returns a linear list of all contained <see cref="VirtualFile"/> objects.
    /// Useful for calculating total archive size or batch processing all files.
    /// </summary>
    /// <returns>A list of all files found in this folder and all nested subfolders.</returns>
    public List<VirtualFile> GetAllFiles()
    {
        List<VirtualFile> filesList = [];

        AddFilesRecursive(this, filesList);

        return filesList;
    }

    /// <summary>
    /// Searches for a specific file throughout the entire folder hierarchy using its unique GUID.
    /// </summary>
    public VirtualFile GetFileById(Guid id)
    {
        return GetAllFiles().Find(f => f.Id == id)!;
    }

    /// <summary>
    /// A helper method that performs a depth-first traversal of the folder tree 
    /// to collect every file into a single linear list.
    /// </summary>
    private void AddFilesRecursive(VirtualFolder folder, List<VirtualFile> filesList)
    {
        filesList.AddRange(folder.Files);

        foreach (var subfolder in folder.Subfolders)
        {
            subfolder.AddFilesRecursive(subfolder, filesList);
        }
    }

    /// <summary>
    /// Serializes the entire folder tree, including all metadata, into a binary format.
    /// Note: This method automatically converts paths to relative format before writing.
    /// </summary>
    /// <returns>A byte array representing the serialized virtual directory structure.</returns>
    public byte[] Serialize()
    {
        SetRelativePaths();

        using var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms, Encoding.UTF8))
        {
            WriteToStream(bw);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Reconstructs a full <see cref="VirtualFolder"/> tree from a binary source (the Archive Catalog).
    /// </summary>
    /// <param name="data">The serialized byte array.</param>
    /// <returns>A populated root <see cref="VirtualFolder"/> instance.</returns>
    public static VirtualFolder Deserialize(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using (var br = new BinaryReader(ms, Encoding.UTF8))
        {
            return ReadFromStream(br);
        }
    }

    /// <summary>
    /// Recursively writes the folder structure, file counts, and subfolder counts to a binary stream.
    /// </summary>
    private void WriteToStream(BinaryWriter bw)
    {
        bw.Write(Path);

        bw.Write(Files.Count);
        foreach (var file in Files)
        {
            file.Serialize(bw);
        }

        bw.Write(Subfolders.Count);
        foreach (var subfolder in Subfolders)
        {
            subfolder.WriteToStream(bw);
        }
    }

    /// <summary>
    /// Recursively reads the folder structure and its hierarchy from a binary stream.
    /// </summary>
    private static VirtualFolder ReadFromStream(BinaryReader br)
    {
        var folder = new VirtualFolder(br.ReadString());

        int filesCount = br.ReadInt32();
        for (int i = 0; i < filesCount; i++)
        {
            folder.Files.Add(
                VirtualFile.Deserialize(br));
        }

        int subfoldersCount = br.ReadInt32();
        for (int i = 0; i < subfoldersCount; i++)
        {
            folder.Subfolders.Add(
                ReadFromStream(br));
        }

        return folder;
    }

    /// <summary>
    /// Checks if a directory exists within the virtual tree based on a relative path string.
    /// </summary>
    public bool IsSubfolderByPath(string path)
    {
        path = path.Replace("\\", "/");

        return FindFolderRecursive(this, path) != null;
    }

    /// <summary>
    /// Finds files by a path string. If the path points to a file, returns that file.
    /// If it points to a folder, returns all files within that folder and its subfolders.
    /// </summary>
    public List<VirtualFile> FindFilesByPath(string path)
    {
        path = path.Replace("\\", "/");

        var file = FindFileRecursive(this, path);
        if (file != null)
            return new List<VirtualFile> { file };

        var folder = FindFolderRecursive(this, path);
        if (folder != null)
            return folder.GetAllFiles();

        return new List<VirtualFile>();
    }

    /// <summary>
    /// Internal recursive search for a specific file by its path.
    /// </summary>
    private VirtualFile? FindFileRecursive(VirtualFolder folder, string path)
    {
        foreach (var file in folder.Files)
        {
            if (file.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                return file;
        }

        foreach (var sub in folder.Subfolders)
        {
            var result = FindFileRecursive(sub, path);
            if (result != null)
                return result;
        }

        return null;
    }

    /// <summary>
    /// Internal recursive search for a folder by its path.
    /// </summary>
    private VirtualFolder? FindFolderRecursive(VirtualFolder folder, string path)
    {
        if (folder.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
            return folder;

        foreach (var sub in folder.Subfolders)
        {
            var result = FindFolderRecursive(sub, path);
            if (result != null)
                return result;
        }

        return null;
    }
}