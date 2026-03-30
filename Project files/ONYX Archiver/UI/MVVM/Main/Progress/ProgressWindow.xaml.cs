using System.Windows;

namespace OnyxArchiver.UI.MVVM.Main.Progress;

/// <summary>
/// Interaction logic for ProgressWindow.xaml.
/// This window serves as a dedicated overlay to display real-time feedback 
/// for long-running operations, such as application updates or batch archiving.
/// </summary>
public partial class ProgressWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressWindow"/> class.
    /// Typically receives its DataContext from the caller (e.g., MainViewModel)
    /// to bind to progress percentages and status messages.
    /// </summary>
    public ProgressWindow()
    {
        InitializeComponent();
    }
}