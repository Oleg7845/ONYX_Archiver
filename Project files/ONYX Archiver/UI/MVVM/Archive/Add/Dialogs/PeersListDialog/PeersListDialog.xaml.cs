using OnyxArchiver.UI.MVVM.Peers;
using System.Windows;
using System.Windows.Controls;

namespace OnyxArchiver.UI.MVVM.Archive.Add.Dialogs.PeersListDialog;

/// <summary>
/// Interaction logic for PeersListDialog.xaml.
/// Provides a modal interface for selecting a recipient from the trusted peers list.
/// Implements incremental loading (pagination) logic during scrolling.
/// </summary>
public partial class PeersListDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PeersListDialog"/> class.
    /// </summary>
    public PeersListDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Monitors the scroll position of the peer grid. 
    /// Triggers a command to fetch more data when the user nears the bottom of the list.
    /// </summary>
    private void PeersGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // Check if the user is scrolling downwards
        if (e.VerticalChange > 0)
        {
            // Calculation: Is the current view within 50 pixels of the total scrollable height?
            // VerticalOffset + ViewportHeight = bottom of the visible area.
            // ExtentHeight = total height of all items (including those hidden).
            if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 50)
            {
                // Communication with ViewModel via DataContext
                if (DataContext is PeersViewModel vm)
                {
                    // Ensure the command is ready to execute (e.g., not already loading)
                    if (vm.LoadDataChunkCommand.CanExecute(null))
                    {
                        vm.LoadDataChunkCommand.Execute(null);
                    }
                }
            }
        }
    }
}