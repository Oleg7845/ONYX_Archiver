using System.Windows;

namespace OnyxArchiver.UI.MVVM.Archive.Add.Dialogs.ArchiveConfiguration;

/// <summary>
/// Interaction logic for ArchiveConfigurationWindow.xaml.
/// This modal dialog allows the user to finalize archive settings 
/// (name, destination, and recipient) before the encryption starts.
/// </summary>
public partial class ArchiveConfigurationWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveConfigurationWindow"/> class.
    /// Typically shown as a Dialog from the AddArchiveViewModel.
    /// </summary>
    public ArchiveConfigurationWindow()
    {
        InitializeComponent();
    }
}