using CommunityToolkit.Mvvm.ComponentModel;
using OnyxArchiver.Core.Models.Archive.VirtualCatalog;
using System.Collections.ObjectModel;

namespace OnyxArchiver.UI.Components;

/// <summary>
/// ViewModel for the ArchiveExplorer component.
/// Manages the state of the currently viewed folder and tracks user selections.
/// </summary>
public partial class ArchiveExplorerViewModel : ObservableObject
{
    private VirtualFolder _currentFolder;

    public ArchiveExplorerViewModel()
    {
        // Update IsAnySelected state whenever the selection collection changes
        SelectedPaths.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsAnySelected));
        };
    }

    /// <summary>
    /// The directory currently being browsed by the user.
    /// Changing this property automatically refreshes the DisplayItems list.
    /// </summary>
    public VirtualFolder CurrentFolder
    {
        get => _currentFolder;
        set
        {
            if (SetProperty(ref _currentFolder, value))
            {
                // Notify the UI that the flattened list of files/folders needs to be re-rendered
                OnPropertyChanged(nameof(DisplayItems));
            }
        }
    }

    /// <summary>
    /// A flattened collection containing both subfolders and files for the CurrentFolder.
    /// This is what the DataGrid/ListView binds to.
    /// </summary>
    public ObservableCollection<object> DisplayItems
    {
        get
        {
            var items = new ObservableCollection<object>();
            if (CurrentFolder != null)
            {
                // Add folders first (standard explorer behavior)
                if (CurrentFolder.Subfolders != null)
                {
                    foreach (var f in CurrentFolder.Subfolders) items.Add(f);
                }

                // Add files second
                if (CurrentFolder.Files != null)
                {
                    foreach (var f in CurrentFolder.Files) items.Add(f);
                }
            }
            return items;
        }
    }

    /// <summary>
    /// List of virtual paths currently selected in the UI.
    /// Used by the ArchiveService for selective extraction.
    /// </summary>
    public ObservableCollection<string> SelectedPaths { get; } = new();

    /// <summary>
    /// Helper property to enable/disable UI elements (like the 'Extract' button) 
    /// based on whether the user has selected anything.
    /// </summary>
    public bool IsAnySelected => SelectedPaths.Count > 0;

    /// <summary>
    /// Forces a UI refresh of the selection state. 
    /// Useful when the selection is modified externally or via complex logic.
    /// </summary>
    public void TriggerSelectionChanged()
    {
        OnPropertyChanged(nameof(IsAnySelected));
    }
}