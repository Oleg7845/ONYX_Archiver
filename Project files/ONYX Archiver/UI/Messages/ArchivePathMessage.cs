namespace OnyxArchiver.UI.Messages;

/// <summary>
/// A messaging record used to transport file or directory paths between ViewModels.
/// Common use cases include passing the result of an OpenFileDialog or 
/// specifying the target location for a new archive.
/// </summary>
/// <param name="Path">The full system path to a file or folder.</param>
public record ArchivePathMessage(string Path);