namespace OnyxArchiver.UI.Abstractions;

/// <summary>
/// A marker interface used to identify ViewModels or Views that should be 
/// displayed in a full-screen mode, typically bypassing the standard 
/// shell/navigation menu layout.
/// </summary>
/// <remarks>
/// Pages implementing this interface (like Login or Registration) signal the 
/// UI coordinator to hide navigation bars, sidebars, or headers.
/// </remarks>
public interface IFullScreenPage {}
