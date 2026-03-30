using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OnyxArchiver.Domain.Interfaces;
using OnyxArchiver.UI.MVVM.Settings.DialogWindows.ChangePassword;
using OnyxArchiver.UI.MVVM.Settings.DialogWindows.DeleteAccount;

namespace OnyxArchiver.UI.MVVM.Settings;

/// <summary>
/// ViewModel responsible for application and user account settings.
/// Acts as a gateway to security-related management tasks like password modification and account deletion.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;

    public SettingsViewModel(IAuthService authService, IUserService userService)
    {
        _authService = authService;
        _userService = userService;
    }

    /// <summary>
    /// Orchestrates the opening of the password change dialog.
    /// Initializes the <see cref="ChangePasswordViewModel"/> and handles window lifecycle management.
    /// </summary>
    [RelayCommand]
    private async Task OpenChangePasswordWindowAsync()
    {
        // Window instantiation (View)
        var dialog = new ChangePasswordWindow();

        // Logical context instantiation (ViewModel)
        var dialogVm = new ChangePasswordViewModel(_userService);

        // Bridge: UI-agnostic ViewModel signals the View to close
        dialogVm.RequestClose += () => dialog.Close();

        dialog.DataContext = dialogVm;
        dialog.Owner = App.Current.MainWindow; // Maintains modal hierarchy
        dialog.ShowDialog(); // Blocks interaction with main window until finished
    }

    /// <summary>
    /// Orchestrates the opening of the account deletion confirmation dialog.
    /// Provides necessary services to the <see cref="DeleteAccountViewModel"/> for secure identity removal.
    /// </summary>
    [RelayCommand]
    private async Task OpenDeleteAccountWindowAsync()
    {
        var dialog = new DeleteAccountWindow();
        var dialogVm = new DeleteAccountViewModel(_authService, _userService);

        dialogVm.RequestClose += () => dialog.Close();

        dialog.DataContext = dialogVm;
        dialog.Owner = App.Current.MainWindow;
        dialog.ShowDialog();
    }
}