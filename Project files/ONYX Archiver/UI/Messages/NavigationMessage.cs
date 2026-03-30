namespace OnyxArchiver.UI.Messages;

/// <summary>
/// Defines the available pages/views in the application for centralized navigation control.
/// </summary>
public enum NavigationPage
{
    /// <summary>The initial entry screen for authentication.</summary>
    Login,

    /// <summary>Screen for creating a new local master account.</summary>
    Registration,

    /// <summary>The workflow for creating and encrypting a new archive.</summary>
    AddArchive,

    /// <summary>The workflow for opening, decrypting, and extracting archives.</summary>
    OpenArchive,

    /// <summary>The contact management screen (handshakes, public keys).</summary>
    Peers,

    /// <summary>User preferences and application configuration.</summary>
    Settings
}

/// <summary>
/// A messaging record dispatched to the Shell or MainViewModel to trigger a view switch.
/// </summary>
/// <param name="Page">The target page to display.</param>
public record NavigationMessage(NavigationPage Page);
