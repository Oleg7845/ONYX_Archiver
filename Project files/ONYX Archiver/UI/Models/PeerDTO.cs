using CommunityToolkit.Mvvm.ComponentModel;

namespace OnyxArchiver.UI.Models;

/// <summary>
/// Data Transfer Object representing a peer in the UI layer.
/// Provides observable properties for seamless WPF data binding.
/// </summary>
public partial class PeerDTO : ObservableObject
{
    /// <summary>
    /// Unique internal database identifier of the peer.
    /// </summary>
    [ObservableProperty]
    private int _id;

    /// <summary>
    /// Unique cryptographic identifier (GUID) of the peer, 
    /// used for network discovery and key mapping.
    /// </summary>
    [ObservableProperty]
    private Guid _peerId;

    /// <summary>
    /// The display name of the contact as shown in the UI.
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// Timestamp indicating when the handshake process was first initiated.
    /// </summary>
    [ObservableProperty]
    private DateTime _createdAt;

    /// <summary>
    /// Timestamp of when the secure key exchange was successfully completed.
    /// Returns null if the peer is still in a pending state.
    /// </summary>
    [ObservableProperty]
    private DateTime? _finalizedAt;

    /// <summary>
    /// Logic property to determine if the cryptographic trust has been fully established.
    /// Bind this to UI elements to show "Verified" badges or status icons.
    /// </summary>
    public bool IsFinalized => FinalizedAt.HasValue;
}