namespace OnyxArchiver.UI.Messages;

/// <summary>
/// A specialized message record used to handle archive creation or addition 
/// triggered from an external context menu (e.g., Windows Shell Extension).
/// This allows the UI to react when a user right-clicks a file in Explorer 
/// and selects "Add to OnyxArchiver".
/// </summary>
/// <param name="Path">The system path to the file or directory that should be archived.</param>
public record AddArchiveFromContextMenuMessage(string Path);