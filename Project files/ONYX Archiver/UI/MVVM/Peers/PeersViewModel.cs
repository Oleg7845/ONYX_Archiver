using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using OnyxArchiver.Domain.Exceptions;
using OnyxArchiver.Domain.Interfaces;
using OnyxArchiver.UI.Messages;
using OnyxArchiver.UI.Models;
using OnyxArchiver.UI.MVVM.Peers.Dialogs.AcceptHandshake;
using OnyxArchiver.UI.MVVM.Peers.Dialogs.CreatePeer;
using OnyxArchiver.UI.MVVM.Peers.Dialogs.UpdatePeer;
using Serilog;
using System.Collections.ObjectModel;
using System.Windows;

namespace OnyxArchiver.UI.MVVM.Peers;

/// <summary>
/// Manages the peer (contact) ecosystem. 
/// Handles asynchronous loading, CRUD, and the multi-step .onxk cryptographic key exchange.
/// </summary>
public partial class PeersViewModel
    : ObservableRecipient,
    IRecipient<CreatePeerMessage>,
    IRecipient<UpdatePeerMessage>,
    IRecipient<AcceptPeerMessage>,
    IRecipient<HandshakePathMessage>
{
    private readonly IAuthService _authService;
    private readonly IPeerService _peersService;

    private int _currentPage = 0;
    private const int PageSize = 50;
    private bool _canLoadMore = true;

    /// <summary> Collection of peers displayed in the view's data grid. </summary>
    [ObservableProperty]
    private ObservableCollection<PeerDTO> _peers = new();

    /// <summary> True when a data fetch is in progress. </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary> Tracks the path of the handshake file currently being processed. </summary>
    [ObservableProperty]
    private string _handshakeFilePath = string.Empty;

    public PeersViewModel(IAuthService authService, IPeerService peersService)
    {
        _authService = authService;
        _peersService = peersService;

        // Initial data load
        _ = LoadDataChunkAsync();

        IsActive = true; // Register as a Messenger recipient
    }

    /// <summary>
    /// Fetches the next 'page' of peers from the database to support infinite scrolling.
    /// </summary>
    [RelayCommand]
    public async Task LoadDataChunkAsync()
    {
        if (IsLoading || !_canLoadMore) return;

        IsLoading = true;
        try
        {
            var batch = await _peersService.GetPeersBatchAsync(_currentPage * PageSize, PageSize);

            if (batch.Any())
            {
                foreach (var peer in batch) Peers.Add(peer);
                _currentPage++;
            }
            else
            {
                _canLoadMore = false;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load peer data chunk.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    #region CRUD Operations

    [RelayCommand]
    private async Task OpenCreationPeerDialogAsync()
    {
        var dialog = new CreatePeerWindow();
        var vm = new CreatePeerViewModel(_peersService);
        vm.RequestClose += () => dialog.Close();
        dialog.DataContext = vm;
        dialog.Owner = App.Current.MainWindow;
        dialog.ShowDialog();
    }

    [RelayCommand]
    private async Task EditAsync(PeerDTO item)
    {
        if (item == null) return;
        var dialog = new UpdatePeerWindow();
        var vm = new UpdatePeerViewModel(_peersService);
        vm.Initialize(item);
        vm.RequestClose += () => dialog.Close();
        dialog.DataContext = vm;
        dialog.Owner = App.Current.MainWindow;
        dialog.ShowDialog();
    }

    [RelayCommand]
    private async Task DeleteAsync(PeerDTO item)
    {
        if (item == null) return;
        try
        {
            await _peersService.DeletePeerAsync(item.Id);
            Peers.Remove(item);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting peer {Id}", item.Id);
        }
    }

    #endregion

    #region Handshake Logic

    /// <summary>
    /// Initiates the 'Acceptance' flow: reads a -begin.onxk file and opens the naming dialog.
    /// </summary>
    [RelayCommand]
    private async Task AcceptHandshakeAsync()
    {
        string filePath = await SelectHandshakeFile("Select Handshake BEGIN file");
        if (string.IsNullOrEmpty(filePath)) return;

        if (!filePath.EndsWith("-begin.onxk", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Please select a valid initiation file (*-begin.onxk)");
            return;
        }

        HandshakeFilePath = filePath;
        await OpenAcceptHandshakeDialogAsync();
    }

    /// <summary>
    /// Processes a -end.onxk file to finalize the key exchange for an existing peer.
    /// </summary>
    [RelayCommand]
    private async Task FinalizeHandshakeAsync()
    {
        try
        {
            string filePath = await SelectHandshakeFile("Select Handshake END file");
            if (string.IsNullOrEmpty(filePath)) return;

            if (!filePath.EndsWith("-end.onxk", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Please select a valid completion file (*-end.onxk)");
                return;
            }

            await _peersService.FinalizeHandshakeAsync(filePath);
            MessageBox.Show("Handshake successfully finalized.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Handshake finalization failed.");
            MessageBox.Show(ex.Message);
        }
    }

    #endregion

    #region Messenger Callbacks

    public void Receive(CreatePeerMessage message) =>
        Application.Current.Dispatcher.Invoke(() => Peers.Add(message.Peer));

    public void Receive(UpdatePeerMessage message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var target = Peers.FirstOrDefault(p => p.Id == message.UpdatedPeer.Id);
            if (target != null)
            {
                int index = Peers.IndexOf(target);
                Peers[index] = message.UpdatedPeer;
            }
        });
    }

    public async void Receive(AcceptPeerMessage message)
    {
        try
        {
            string exportPath = await SelectHandshakeExportFolder();
            if (string.IsNullOrWhiteSpace(exportPath)) return;

            await _peersService.AcceptHandshakeAsync(message.PeerName, HandshakeFilePath, exportPath);
            MessageBox.Show($"Response file generated in: {exportPath}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to accept handshake.");
            MessageBox.Show(ex.Message);
        }
    }

    /// <summary>
    /// Handles files dropped into the app or opened via file association.
    /// Automatically detects if it's a 'Begin' or 'End' handshake file.
    /// </summary>
    public void Receive(HandshakePathMessage message)
    {
        string path = message.Path;
        if (path.EndsWith("-begin.onxk", StringComparison.OrdinalIgnoreCase))
        {
            HandshakeFilePath = path;
            _ = OpenAcceptHandshakeDialogAsync();
        }
        else if (path.EndsWith("-end.onxk", StringComparison.OrdinalIgnoreCase))
        {
            _ = _peersService.FinalizeHandshakeAsync(path);
        }
    }

    #endregion

    #region Dialog Helpers

    private async Task OpenAcceptHandshakeDialogAsync()
    {
        var dialog = new AcceptHandshakeWindow();
        var vm = new AcceptHandshakeViewModel();
        vm.RequestClose += () => dialog.Close();
        dialog.DataContext = vm;
        dialog.Owner = App.Current.MainWindow;
        dialog.ShowDialog();
    }

    private async Task<string> SelectHandshakeFile(string title)
    {
        var dialog = new OpenFileDialog { Title = title, Filter = "Onyx Handshake (*.onxk)|*.onxk" };
        return dialog.ShowDialog() == true ? dialog.FileName : string.Empty;
    }

    private async Task<string> SelectHandshakeExportFolder()
    {
        var dialog = new OpenFolderDialog { Title = "Select folder to save response file" };
        return dialog.ShowDialog() == true ? dialog.FolderName : string.Empty;
    }

    #endregion
}