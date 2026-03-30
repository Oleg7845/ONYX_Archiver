using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OnyxArchiver.Domain.Exceptions;
using OnyxArchiver.Domain.Interfaces;
using OnyxArchiver.UI.Messages;
using OnyxArchiver.UI.Models;
using Serilog;

namespace OnyxArchiver.UI.MVVM.Peers.Dialogs.UpdatePeer;

/// <summary>
/// ViewModel for the peer information update dialog.
/// Orchestrates the modification of existing peer metadata and ensures UI-wide consistency.
/// </summary>
public partial class UpdatePeerViewModel : ObservableObject
{
    private readonly IPeerService _peerService;

    /// <summary> Event raised to signal the View (Window) to close. </summary>
    public event Action? RequestClose;

    /// <summary> The peer data transfer object currently being edited. </summary>
    [ObservableProperty]
    private PeerDTO _peer;

    /// <summary> 
    /// Temporary storage for the new name. 
    /// Prevents the UI from updating the main list until 'Confirm' is pressed. 
    /// </summary>
    [ObservableProperty]
    private string _peerName = string.Empty;

    /// <summary> UI lock state to prevent concurrent update requests. </summary>
    [ObservableProperty]
    private bool _isLocked;

    /// <summary> Status or validation error message for the dialog. </summary>
    [ObservableProperty]
    private string _error = string.Empty;

    public UpdatePeerViewModel(IPeerService peerService)
    {
        _peerService = peerService;
    }

    /// <summary>
    /// Populates the ViewModel with existing peer data.
    /// </summary>
    /// <param name="peer">The peer record selected for editing.</param>
    public void Initialize(PeerDTO peer)
    {
        Peer = peer;
        PeerName = Peer.Name; // Initialize temporary field
    }

    /// <summary>
    /// Persists the name change to the database and broadcasts the update.
    /// </summary>
    [RelayCommand]
    private async Task ConfirmAsync()
    {
        Error = string.Empty;

        if (string.IsNullOrWhiteSpace(PeerName))
        {
            Error = "Peer name cannot be empty.";
            return;
        }

        IsLocked = true;

        try
        {
            // Apply the change to the DTO
            Peer.Name = PeerName;

            // Update persistence layer
            await _peerService.UpdatePeerNameAsync(Peer);

            // Notify the rest of the application via Messenger
            WeakReferenceMessenger.Default.Send(new UpdatePeerMessage(Peer));

            // Close the dialog on success
            RequestClose?.Invoke();
        }
        catch (PeerUpdatingException ex)
        {
            Error = ex.UserMessage;
            Log.Warning("Failed to update peer: {Message}", ex.Message);
        }
        catch (DomainException ex)
        {
            Error = ex.UserMessage;
            Log.Error(ex, "Domain logic error during peer update.");
        }
        catch (Exception ex)
        {
            Error = "An unexpected error occurred.";
            Log.Fatal(ex, "Critical failure in UpdatePeerViewModel.");
        }
        finally
        {
            IsLocked = false;
        }
    }
}