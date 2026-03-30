using OnyxArchiver.Core.Models.Archive.VirtualCatalog;
using System.IO;

namespace OnyxArchiver.Core.IO;

/// <summary>
/// Provides functionality to scan the file system and create a virtual representation of folders and files.
/// </summary>
public class FolderScanner
{
    /// <summary>
    /// Recursively scans a directory and builds a <see cref="VirtualFolder"/> structure.
    /// </summary>
    /// <param name="path">The full path to the directory to scan.</param>
    /// <returns>A <see cref="VirtualFolder"/> containing all subfolders and files found in the specified path.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the specified path does not exist.</exception>
    public static VirtualFolder ScanFolder(string path)
    {
        VirtualFolder folder = new VirtualFolder(path);

        foreach (string filePath in Directory.GetFiles(path))
        {
            FileInfo fileInfo = new FileInfo(filePath);
            folder.Files.Add(
                new VirtualFile(fileInfo.FullName, fileInfo.Length));
        }

        foreach (string subfolderPath in Directory.GetDirectories(path))
        {
            folder.Subfolders.Add(
                ScanFolder(subfolderPath));
        }

        return folder;
    }

    /// <summary>
    /// Scans a single file and wraps it in a <see cref="VirtualFolder"/> container.
    /// </summary>
    /// <param name="filePath">The full path to the file to scan.</param>
    /// <returns>A <see cref="VirtualFolder"/> containing the single specified file.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    public static VirtualFolder ScanFile(string filePath)
    {
        VirtualFolder folder = new VirtualFolder(Path.GetDirectoryName(filePath)!);

        FileInfo fileInfo = new FileInfo(filePath);
        folder.Files.Add(
            new VirtualFile(fileInfo.FullName, fileInfo.Length));

        return folder;
    }
}
