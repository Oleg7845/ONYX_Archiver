using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OnyxArchiver.Domain.Interfaces;
using OnyxArchiver.UI.Messages;
using OnyxArchiver.UI.Models;
using Serilog;
using System.Collections.ObjectModel;

namespace OnyxArchiver.UI.MVVM.Archive.Add.Dialogs.PeersListDialog;

/// <summary>
/// ViewModel for the peer selection dialog. 
/// Manages incremental data loading (pagination) and selection broadcasting.
/// </summary>
public partial class PeersListDialogViewModel : ObservableObject
{
    /// <summary> Event to signal the View (Window) to close itself. </summary>
    public event Action? RequestClose;

    private readonly IPeerService _peerService;
    private int _currentPage = 0;
    private const int PageSize = 50;
    private bool _canLoadMore = true;

    /// <summary> Dynamic collection of peers bound to the UI (DataGrid/ListView). </summary>
    [ObservableProperty]
    private ObservableCollection<PeerDTO> _peers = new();

    /// <summary> Flag used to show/hide a loading spinner in the UI. </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary> The peer currently highlighted or clicked by the user. </summary>
    [ObservableProperty]
    private PeerDTO? _selectedPeer;

    public PeersListDialogViewModel(IPeerService peersService)
    {
        _peerService = peersService;

        // Initial data fetch
        _ = LoadDataChunkAsync();
    }

    /// <summary>
    /// Fetches a specific chunk (page) of peer data from the database.
    /// Prevents concurrent loading using the IsLoading flag.
    /// </summary>
    [RelayCommand]
    public async Task LoadDataChunkAsync()
    {
        // Guard clause: stop if already loading or if the end of the list was reached
        if (IsLoading || !_canLoadMore) return;

        IsLoading = true;

        try
        {
            // Requesting data with Offset/Limit strategy (standard SQL pagination)
            List<PeerDTO> peersBatch = await _peerService.GetPeersBatchAsync(
                offset: _currentPage * PageSize,
                limit: PageSize
            );

            if (peersBatch.Count > 0)
            {
                // Add new items to the existing collection to trigger UI updates
                foreach (var peer in peersBatch)
                {
                    Peers.Add(peer);
                }
                _currentPage++;
            }

            // If the batch is smaller than PageSize, we've likely reached the end
            if (peersBatch.Count < PageSize)
            {
                _canLoadMore = false;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load peer data chunk at page {Page}", _currentPage);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Automatically triggered by CommunityToolkit.Mvvm when SelectedPeer changes.
    /// Broadcasts the selection and requests dialog closure.
    /// </summary>
    partial void OnSelectedPeerChanged(PeerDTO? value)
    {
        if (value != null)
        {
            // Send the selected peer to the subscriber (usually ArchiveConfigurationViewModel)
            WeakReferenceMessenger.Default.Send(new SelectedPeerMessage(value));

            // Close the dialog window
            RequestClose?.Invoke();
        }
    }
}