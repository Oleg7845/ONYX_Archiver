using System.Windows.Controls;

namespace OnyxArchiver.UI.MVVM.Peers;

/// <summary>
/// Interaction logic for PeersView.xaml.
/// Displays a comprehensive list of trusted peers with support for 
/// incremental data loading (pagination) during scrolling.
/// </summary>
public partial class PeersView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PeersView"/> class.
    /// </summary>
    public PeersView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Event handler for the peer grid's scroll changes. 
    /// Detects when the user is nearing the bottom of the list and triggers a background data fetch.
    /// </summary>
    private void PeersGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // Only trigger when scrolling downwards
        if (e.VerticalChange > 0)
        {
            // Calculation: Check if the visible 'viewport' is within 50 pixels of the total content height.
            // VerticalOffset (current top position) + ViewportHeight (visible area) vs ExtentHeight (total height).
            if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 50)
            {
                // Access the ViewModel via DataContext to execute the data loading command
                if (DataContext is PeersViewModel vm)
                {
                    // Ensure the command logic allows execution (e.g., checks if already loading or reached end)
                    if (vm.LoadDataChunkCommand.CanExecute(null))
                    {
                        vm.LoadDataChunkCommand.Execute(null);
                    }
                }
            }
        }
    }
}