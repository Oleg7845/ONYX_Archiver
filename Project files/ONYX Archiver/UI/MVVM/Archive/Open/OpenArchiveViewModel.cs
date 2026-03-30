using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using OnyxArchiver.Core.Models.Archive.VirtualCatalog;
using OnyxArchiver.Domain.Exceptions;
using OnyxArchiver.Domain.Interfaces;
using OnyxArchiver.UI.Components;
using OnyxArchiver.UI.Messages;
using Serilog;
using System.IO;
using System.Windows;

namespace OnyxArchiver.UI.MVVM.Archive.Open;

/// <summary>
/// ViewModel responsible for managing the archive decryption and extraction process.
/// Handles metadata loading, virtual file exploration, and targeted extraction.
/// </summary>
public partial class OpenArchiveViewModel
    : ObservableRecipient,
    IRecipient<ArchivePathMessage>
{
    private readonly IAuthService _authService;
    private readonly IArchiveService _archiveService;

    public ArchiveExplorerViewModel ExplorerVM { get; } = new();

    /// <summary> Full local path to the encrypted .onx archive. </summary>
    [ObservableProperty]
    private string _archivePath = string.Empty;

    [ObservableProperty]
    private string _archiveName = string.Empty;

    /// <summary> UI State: True if a file is loaded and ready for extraction. </summary>
    [ObservableProperty]
    private bool _isArchivePathSelected;

    /// <summary> UI State: Controls the visibility of the file explorer tree. </summary>
    [ObservableProperty]
    private bool _isArchiveExplorerOpened;

    /// <summary> The logical structure of the archive retrieved from its header. </summary>
    [ObservableProperty]
    private VirtualFolder? _virtualFolder;

    /// <summary> Real-time decryption progress (0-100). </summary>
    [ObservableProperty]
    private double _archiveProgress;

    /// <summary> Text description of the file currently being processed. </summary>
    [ObservableProperty]
    private string _currentStatusText = string.Empty;

    [ObservableProperty]
    private CancellationTokenSource _cancellationTokenSrc = new();

    public OpenArchiveViewModel(IAuthService authService, IArchiveService archiveService)
    {
        _authService = authService;
        _archiveService = archiveService;

        IsActive = true; // Required for IRecipient messages
    }

    /// <summary>
    /// Loads the archive, decrypts the header, and populates the virtual file tree.
    /// </summary>
    [RelayCommand]
    private async Task SetArchivePathAsync(string path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                ArchivePath = path;
                ArchiveName = Path.GetFileNameWithoutExtension(ArchivePath);

                // Decrypt and load metadata without extracting the whole content
                VirtualFolder = await _archiveService.LoadArchiveMetadataAsync(ArchivePath);

                IsArchivePathSelected = true;
                IsArchiveExplorerOpened = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load archive metadata for: {Path}", path);
                MessageBox.Show("Could not open archive. It may be corrupted or encrypted for another user.");
            }
        }
    }

    /// <summary>
    /// Aborts the current extraction process.
    /// </summary>
    [RelayCommand]
    private void CancelUnpacking()
    {
        if (CancellationTokenSrc != null && !CancellationTokenSrc.IsCancellationRequested)
            CancellationTokenSrc.Cancel();
    }

    /// <summary>
    /// Helper to show the native Windows folder picker.
    /// </summary>
    private async Task<string> GetUnpackedDataPathAsync()
    {
        var dialog = new OpenFolderDialog { Title = "Select Extraction Destination" };
        return dialog.ShowDialog() == true ? dialog.FolderName : string.Empty;
    }

    /// <summary>
    /// Extracts every file from the archive to the selected folder.
    /// </summary>
    [RelayCommand]
    private async Task FullUnpackingAsync()
    {
        if (string.IsNullOrWhiteSpace(ArchivePath)) return;

        string destinationPath = await GetUnpackedDataPathAsync();
        if (string.IsNullOrWhiteSpace(destinationPath)) return;

        await ExecuteUnpackingAsync(async (progress) =>
        {
            await _archiveService.UpackFullArchiveAsync(destinationPath, CancellationTokenSrc, progress);
        });
    }

    /// <summary>
    /// Extracts only the specifically selected files/folders.
    /// </summary>
    [RelayCommand]
    private async Task SelectiveUnpackingAsync(IEnumerable<string> paths)
    {
        if (string.IsNullOrWhiteSpace(ArchivePath) || paths == null || !paths.Any()) return;

        string destinationPath = await GetUnpackedDataPathAsync();
        if (string.IsNullOrWhiteSpace(destinationPath)) return;

        await ExecuteUnpackingAsync(async (progress) =>
        {
            await _archiveService.UpackArchiveSelectiveAsync(paths, destinationPath, CancellationTokenSrc, progress);
        });
    }

    /// <summary>
    /// Encapsulates common logic for progress reporting and error handling during extraction.
    /// </summary>
    private async Task ExecuteUnpackingAsync(Func<IProgress<OnyxArchiver.Core.Models.Archive.ProgressReport>, Task> action)
    {
        CancellationTokenSrc = new CancellationTokenSource();
        var progressWindow = new Open.Progress.ProgressWindow
        {
            DataContext = this,
            Owner = Application.Current.MainWindow
        };
        progressWindow.Show();

        var progressIndicator = new Progress<OnyxArchiver.Core.Models.Archive.ProgressReport>(report => {
            ArchiveProgress = report.Percentage;
            CurrentStatusText = $"Unpacking: {report.CurrentFile} ({report.Percentage:F1}%)";
        });

        try
        {
            await action(progressIndicator);
        }
        catch (DomainException ex)
        {
            Log.Error(ex, "Domain error during extraction");
            MessageBox.Show(ex.UserMessage);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unexpected extraction failure");
            MessageBox.Show(ex.Message);
        }
        finally
        {
            progressWindow.Close();
            CancellationTokenSrc.Dispose();
        }
    }

    [RelayCommand]
    private async Task CloseArchiveExplorer()
    {
        await ClearInternalData();
    }

    private async Task ClearInternalData()
    {
        await _archiveService.ClearMetadata();
        ArchivePath = string.Empty;
        IsArchivePathSelected = false;
        IsArchiveExplorerOpened = false;
        VirtualFolder = null;
    }

    /// <summary>
    /// Receiver for external file-open events (e.g. from Command Line or DragDrop).
    /// </summary>
    public void Receive(ArchivePathMessage message)
    {
        _ = SetArchivePathAsync(message.Path);
    }
}