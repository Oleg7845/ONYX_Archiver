using System.Windows;

namespace OnyxArchiver.UI.MVVM.Settings.DialogWindows.DeleteAccount;

/// <summary>
/// Interaction logic for DeleteAccountWindow.xaml.
/// This modal dialog serves as a final confirmation point before 
/// permanently removing a user's identity and associated local keys.
/// </summary>
public partial class DeleteAccountWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteAccountWindow"/> class.
    /// Expected to be launched from <see cref="SettingsViewModel"/>.
    /// </summary>
    public DeleteAccountWindow()
    {
        InitializeComponent();
    }
}