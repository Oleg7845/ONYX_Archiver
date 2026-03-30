using System.Windows;

namespace OnyxArchiver.UI.MVVM.Archive.Open.Progress;

/// <summary>
/// Interaction logic for ProgressWindow.xaml.
/// This window provides visual feedback during the archive decryption and 
/// extraction process, showing progress percentages and the current file path.
/// </summary>
public partial class ProgressWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressWindow"/> class.
    /// Expects a DataContext of type <see cref="OpenArchiveViewModel"/>.
    /// </summary>
    public ProgressWindow()
    {
        InitializeComponent();
    }
}