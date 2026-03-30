using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OnyxArchiver.Domain.Exceptions;
using OnyxArchiver.Domain.Interfaces;
using OnyxArchiver.UI.Messages;
using OnyxArchiver.UI.Models;
using OnyxArchiver.UI.MVVM.Archive.Add.Dialogs.ArchiveConfiguration;
using OnyxArchiver.UI.MVVM.Archive.Add.Progress;
using Serilog;
using System.Windows;

namespace OnyxArchiver.UI.MVVM.Archive.Add;

/// <summary>
/// ViewModel responsible for the archive creation workflow.
/// Manages file selection, recipient (peer) listing, and real-time encryption progress.
/// Implements IRecipient to react to system-wide messages like file drops.
/// </summary>
public partial class AddArchiveViewModel
    : ObservableRecipient,
    IRecipient<PathMessage>,
    IRecipient<ArchiveConfigurationMessage>
{
    private readonly IAuthService _authService;
    private readonly IPeerService _peerService;
    private readonly IArchiveService _archiveService;

    /// <summary> Full path of the source file or directory to be compressed/encrypted. </summary>
    [ObservableProperty]
    private string _sourcePath = string.Empty;

    /// <summary> Final filename for the created archive. </summary>
    [ObservableProperty]
    private string _archiveName = string.Empty;

    /// <summary> Target directory where the resulting .onx file will be stored. </summary>
    [ObservableProperty]
    private string _archivePath = string.Empty;

    /// <summary> The selected recipient (Peer) whose public key will be used for encryption. </summary>
    [ObservableProperty]
    private PeerDTO? _selectedPeer;

    /// <summary> Current progress percentage (0.0 to 100.0) bound to the UI ProgressBar. </summary>
    [ObservableProperty]
    private double _archiveProgress;

    /// <summary> Human-readable status update (e.g., "Processing: file.txt"). </summary>
    [ObservableProperty]
    private string _currentStatusText = string.Empty;

    /// <summary> Token source to allow the user to gracefully abort the encryption thread. </summary>
    [ObservableProperty]
    private CancellationTokenSource _cancellationTokenSrc = new();

    public AddArchiveViewModel(IAuthService authService, IPeerService peerService, IArchiveService archiveService)
    {
        _authService = authService;
        _peerService = peerService;
        _archiveService = archiveService;

        // Active registers this ViewModel as a recipient in the Messenger bus
        IsActive = true;
    }

    /// <summary>
    /// Triggered when a path is provided (via FilePicker or DragDrop).
    /// Initiates the configuration phase.
    /// </summary>
    [RelayCommand]
    private async Task SetSourcePathAsync(string path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            SourcePath = path;
            await OpenArchiveConfigurationDialogAsync();
        }
    }

    /// <summary>
    /// Cancels the ongoing archive creation by signaling the cancellation token.
    /// </summary>
    [RelayCommand]
    private void CancelArchiving()
    {
        if (CancellationTokenSrc != null && !CancellationTokenSrc.IsCancellationRequested)
            CancellationTokenSrc.Cancel();
    }

    /// <summary>
    /// Displays a modal dialog for the user to select the recipient and archive name.
    /// </summary>
    private async Task OpenArchiveConfigurationDialogAsync()
    {
        var dialog = new ArchiveConfigurationWindow();
        var dialogVm = new ArchiveConfigurationViewModel(SourcePath, _peerService);

        // Simple event-to-close bridge
        dialogVm.RequestClose += () => dialog.Close();

        dialog.DataContext = dialogVm;
        dialog.Owner = Application.Current.MainWindow;
        dialog.ShowDialog();
    }

    /// <summary>
    /// The main execution logic for archive creation.
    /// Orchestrates background service calls and UI progress window management.
    /// </summary>
    private async Task StartArchivingAsync()
    {
        if (SelectedPeer == null || string.IsNullOrWhiteSpace(SourcePath)) return;

        if (string.IsNullOrWhiteSpace(ArchivePath))
        {
            ResetAllFields();
            return;
        }

        CancellationTokenSrc = new CancellationTokenSource();

        // Show a non-blocking progress window tied to this VM's properties
        var progressWindow = new ProgressWindow
        {
            DataContext = this,
            Owner = Application.Current.MainWindow
        };
        progressWindow.Show();

        // Bridge between the domain service progress and WPF UI thread
        var progressIndicator = new Progress<OnyxArchiver.Core.Models.Archive.ProgressReport>(report => {
            ArchiveProgress = report.Percentage;
            CurrentStatusText = $"Packing: {report.CurrentFile} ({report.Percentage:F1}%)";
        });

        try
        {
            // Call the infrastructure layer to perform actual IO/Crypto operations
            await _archiveService.CreateArchiveAsync(
                peerId: SelectedPeer.Id,
                sourcePath: SourcePath,
                destinationPath: ArchivePath,
                archiveName: ArchiveName,
                cancellationTokenSrc: CancellationTokenSrc,
                progress: progressIndicator);
        }
        catch (DomainException ex)
        {
            Log.Error(ex, "Domain error during archiving: {Path}", SourcePath);
            MessageBox.Show(ex.UserMessage, "Archiving Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unexpected crash during archiving");
            MessageBox.Show("An unexpected error occurred.", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Stop);
        }
        finally
        {
            progressWindow.Close();
            CancellationTokenSrc.Dispose();
            ResetAllFields();
        }
    }

    private void ResetAllFields()
    {
        SourcePath = string.Empty;
        ArchiveName = string.Empty;
        ArchivePath = string.Empty;
        SelectedPeer = null;
    }

    /// <summary>
    /// Handles PathMessage (e.g., from Windows Explorer via SingleInstance pipe).
    /// </summary>
    public void Receive(PathMessage message)
    {
        _ = SetSourcePathAsync(message.Path);
    }

    /// <summary>
    /// Handles ArchiveConfigurationMessage (sent after user clicks 'OK' in the config dialog).
    /// </summary>
    public void Receive(ArchiveConfigurationMessage message)
    {
        ArchiveName = message.ArchiveName;
        ArchivePath = message.ArchivePath;
        SelectedPeer = message.SelectedPeer;

        _ = StartArchivingAsync();
    }
}