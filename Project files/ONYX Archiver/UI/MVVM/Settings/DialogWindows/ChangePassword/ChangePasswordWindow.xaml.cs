using System.Windows;

namespace OnyxArchiver.UI.MVVM.Settings.DialogWindows.ChangePassword;

/// <summary>
/// Interaction logic for ChangePasswordWindow.xaml.
/// This modal dialog provides a secure interface for authenticated users 
/// to update their account credentials.
/// </summary>
public partial class ChangePasswordWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangePasswordWindow"/> class.
    /// Expected to be initialized with a <see cref="ChangePasswordViewModel"/> 
    /// from the Settings panel.
    /// </summary>
    public ChangePasswordWindow()
    {
        InitializeComponent();
    }
}