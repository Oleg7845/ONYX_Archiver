using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OnyxArchiver.UI.Messages;

namespace OnyxArchiver.UI.MVVM.Peers.Dialogs.AcceptHandshake;

/// <summary>
/// ViewModel for the handshake acceptance dialog.
/// Captures the local display name for a new peer during the cryptographic key exchange.
/// </summary>
public partial class AcceptHandshakeViewModel : ObservableObject
{
    /// <summary> Event to signal the View (Window) to close. </summary>
    public event Action? RequestClose;

    /// <summary> The local alias chosen for the incoming peer request. </summary>
    [ObservableProperty]
    private string _peerName = string.Empty;

    /// <summary> Locks the UI during message dispatch. </summary>
    [ObservableProperty]
    private bool _isLocked;

    /// <summary> Validation error message for the UI. </summary>
    [ObservableProperty]
    private string _error = string.Empty;

    /// <summary>
    /// Validates the chosen name and broadcasts an <see cref="AcceptPeerMessage"/>.
    /// The actual file generation is handled by the subscriber (PeersViewModel).
    /// </summary>
    [RelayCommand]
    private async Task ConfirmAsync()
    {
        Error = string.Empty;

        if (string.IsNullOrWhiteSpace(PeerName))
        {
            Error = "Please assign a name to this contact.";
            return;
        }

        IsLocked = true;

        try
        {
            // Dispatches the name to the main PeersViewModel to trigger file generation
            WeakReferenceMessenger.Default.Send(new AcceptPeerMessage(PeerName));

            // Close the dialog immediately after sending the message
            RequestClose?.Invoke();
        }
        finally
        {
            IsLocked = false;
        }
    }
}