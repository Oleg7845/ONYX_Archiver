using System.Windows;

namespace OnyxArchiver.UI.MVVM.Archive.Add.Progress;

/// <summary>
/// Interaction logic for ProgressWindow.xaml.
/// This window provides a modal visual representation of background tasks 
/// such as file encryption, compression, or cloud uploads.
/// </summary>
public partial class ProgressWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressWindow"/> class.
    /// Expects an external DataContext (usually AddArchiveViewModel) to be 
    /// injected before calling .Show().
    /// </summary>
    public ProgressWindow()
    {
        InitializeComponent();
    }
}