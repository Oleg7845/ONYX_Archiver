using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using OnyxArchiver.Domain.Interfaces;
using OnyxArchiver.UI.Messages;
using OnyxArchiver.UI.Models;

namespace OnyxArchiver.UI.MVVM.Archive.Add.Dialogs.ArchiveConfiguration;

/// <summary>
/// ViewModel for final archive setup. 
/// Handles file naming, destination selection, and recipient assignment.
/// </summary>
public partial class ArchiveConfigurationViewModel
    : ObservableRecipient,
    IRecipient<SelectedPeerMessage>
{
    /// <summary> Event to signal the View to close this configuration window. </summary>
    public event Action? RequestClose;

    private readonly IPeerService _peerService;

    /// <summary> The desired name of the resulting archive file. </summary>
    [ObservableProperty]
    private string _archiveName = string.Empty;

    /// <summary> The directory path where the archive will be generated. </summary>
    [ObservableProperty]
    private string _archivePath = string.Empty;

    /// <summary> The Data Transfer Object of the chosen recipient. </summary>
    [ObservableProperty]
    private PeerDTO? _selectedPeer;

    /// <summary> Display-friendly name of the selected peer for UI binding. </summary>
    [ObservableProperty]
    private string _selectedPeerName = "None selected";

    /// <summary> Validation error message to be displayed in the UI (e.g., in a Red label). </summary>
    [ObservableProperty]
    private string _error = string.Empty;

    public ArchiveConfigurationViewModel(string sourcePath, IPeerService peerService)
    {
        _peerService = peerService;

        // Default the archive name to the source folder/file name for convenience
        ArchiveName = System.IO.Path.GetFileNameWithoutExtension(sourcePath);

        // Essential: Registers this instance to listen for SelectedPeerMessage
        IsActive = true;
    }

    /// <summary>
    /// Invokes the native Windows folder selection dialog.
    /// </summary>
    [RelayCommand]
    private async Task GetArchivePathAsync()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Destination Folder"
        };

        if (dialog.ShowDialog() == true)
        {
            ArchivePath = dialog.FolderName;
        }
    }

    /// <summary>
    /// Opens the sub-dialog to browse and select a trusted peer.
    /// </summary>
    [RelayCommand]
    private async Task OpenPeersListDialogAsync()
    {
        var dialog = new PeersListDialog.PeersListDialog();
        var dialogVm = new PeersListDialog.PeersListDialogViewModel(_peerService);

        // Simple closure bridge
        dialogVm.RequestClose += () => dialog.Close();

        dialog.DataContext = dialogVm;
        dialog.Owner = App.Current.MainWindow;
        dialog.ShowDialog();
    }

    /// <summary>
    /// Validates inputs and broadcasts the final configuration to the application.
    /// </summary>
    [RelayCommand]
    private async Task ConfirmArchiveAsync()
    {
        Error = string.Empty;

        // Basic validation logic
        if (string.IsNullOrWhiteSpace(ArchiveName))
        {
            Error = "Please enter an archive name.";
            return;
        }

        if (string.IsNullOrWhiteSpace(ArchivePath))
        {
            Error = "Please select a destination path.";
            return;
        }

        if (SelectedPeer == null)
        {
            Error = "Please select a recipient (peer).";
            return;
        }

        // Send the finalized data back to AddArchiveViewModel or ArchiveService
        WeakReferenceMessenger.Default.Send(
            new ArchiveConfigurationMessage(
                ArchiveName: ArchiveName,
                ArchivePath: ArchivePath,
                SelectedPeer: SelectedPeer));

        // Signal UI to close the dialog
        RequestClose?.Invoke();
    }

    /// <summary>
    /// Implementation of IRecipient. Updates the selected peer 
    /// when the user picks one from the PeersListDialog.
    /// </summary>
    public void Receive(SelectedPeerMessage message)
    {
        SelectedPeer = message.Peer;
        SelectedPeerName = message.Peer.Name;

        // Clear error if it was previously related to peer selection
        if (Error == "Select peer") Error = string.Empty;
    }
}