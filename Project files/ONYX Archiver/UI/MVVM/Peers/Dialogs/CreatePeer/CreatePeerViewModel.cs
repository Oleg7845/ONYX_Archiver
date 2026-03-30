using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OnyxArchiver.Domain.Exceptions;
using OnyxArchiver.Domain.Interfaces;
using Serilog;

namespace OnyxArchiver.UI.MVVM.Peers.Dialogs.CreatePeer;

/// <summary>
/// ViewModel for the peer creation dialog.
/// Orchestrates the generation of a new cryptographic identity for a contact 
/// and exports the initial handshake file to the local file system.
/// </summary>
public partial class CreatePeerViewModel : ObservableObject
{
    private readonly IPeerService _peerService;

    /// <summary> Event to signal the View (Window) to close upon successful export. </summary>
    public event Action? RequestClose;

    /// <summary> The descriptive name assigned to the new trusted contact. </summary>
    [ObservableProperty]
    private string _peerName = string.Empty;

    /// <summary> 
    /// Locks the UI to prevent duplicate key generation requests. 
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private bool _isLocked;

    /// <summary> Validation error or service-level failure message for the UI. </summary>
    [ObservableProperty]
    private string _error = string.Empty;

    public CreatePeerViewModel(IPeerService peerService)
    {
        _peerService = peerService;
    }

    /// <summary>
    /// Starts the peer creation workflow.
    /// Prompts for a destination folder, then calls the service to generate RSA/Ed25519 keys.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private async Task ConfirmAsync()
    {
        Error = string.Empty;

        // 1. Validate local inputs
        if (string.IsNullOrWhiteSpace(PeerName))
        {
            Error = "Please enter a name for this peer.";
            return;
        }

        // 2. Request destination for the public key file
        string handshakeFolderPath = await SelectHandshakeExportFolder();

        if (string.IsNullOrWhiteSpace(handshakeFolderPath))
        {
            // Error is intentionally generic as the user simply cancelled the dialog
            return;
        }

        IsLocked = true;

        try
        {
            // 3. Perform cryptographic operations and file export via Domain Service
            await _peerService.CreateKeyExchangeFileAsync(PeerName, handshakeFolderPath);

            // 4. Success: Close the dialog
            RequestClose?.Invoke();
        }
        catch (PeerCreatingException ex)
        {
            Error = ex.UserMessage;
            Log.Warning("Handshake export failed: {Message}", ex.Message);
        }
        catch (DomainException ex)
        {
            Error = ex.UserMessage;
            Log.Error(ex, "Domain logic error during peer creation.");
        }
        catch (Exception ex)
        {
            Error = "A critical error occurred while creating the peer.";
            Log.Fatal(ex, "Unexpected failure in CreatePeerViewModel.");
        }
        finally
        {
            IsLocked = false;
        }
    }

    /// <summary> Guard for the Confirm command. </summary>
    private bool CanConfirm => !IsLocked;

    /// <summary>
    /// Invokes the native Windows folder selection dialog.
    /// </summary>
    private async Task<string> SelectHandshakeExportFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Folder to Export Handshake File",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : string.Empty;
    }
}