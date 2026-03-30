using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OnyxArchiver.Domain.Exceptions;
using OnyxArchiver.Domain.Interfaces;
using OnyxArchiver.UI.Messages;
using Serilog;

namespace OnyxArchiver.UI.MVVM.Settings.DialogWindows.DeleteAccount;

/// <summary>
/// ViewModel for the account deletion confirmation dialog.
/// Enforces identity verification via password before performing irreversible data removal.
/// </summary>
public partial class DeleteAccountViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;

    /// <summary> Event to signal the View (Window) to close. </summary>
    public event Action? RequestClose;

    /// <summary> The user's current password, used as a final authorization token. </summary>
    [ObservableProperty]
    private string _password = string.Empty;

    /// <summary> 
    /// Locks the UI during the deletion process to prevent duplicate requests. 
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private bool _isLocked;

    /// <summary> Validation error or security exception message for the UI. </summary>
    [ObservableProperty]
    private string _error = string.Empty;

    public DeleteAccountViewModel(IAuthService authService, IUserService userService)
    {
        _authService = authService;
        _userService = userService;
    }

    /// <summary>
    /// Executes the permanent account deletion sequence.
    /// Order: Verification -> Deletion -> Session Cleanup -> Navigation.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private async Task ConfirmAsync()
    {
        Error = string.Empty;

        if (string.IsNullOrWhiteSpace(Password))
        {
            Error = "Please enter your password to confirm.";
            return;
        }

        IsLocked = true;

        try
        {
            // 1. Irreversible removal of user record and local keys
            await _userService.DeleteUserAsync(password: Password);

            // 2. Clear current authentication context
            await _authService.Logout();

            // 3. Force navigation back to the Login screen
            WeakReferenceMessenger.Default.Send(new NavigationMessage(NavigationPage.Login));

            // 4. Close the confirmation dialog
            RequestClose?.Invoke();
        }
        catch (UserNotFoundException ex)
        {
            Error = ex.UserMessage;
            Log.Warning("Account deletion failed: User context not found.");
        }
        catch (InvalidPasswordException ex)
        {
            Error = ex.UserMessage;
            Log.Warning("Account deletion rejected: Incorrect password provided.");
        }
        catch (UserDeletingException ex)
        {
            Error = ex.UserMessage;
            Log.Error(ex, "Server-side error during account deletion.");
        }
        catch (DomainException ex)
        {
            Error = ex.UserMessage;
        }
        catch (Exception ex)
        {
            Error = "A critical error occurred while deleting your account.";
            Log.Fatal(ex, "Unexpected failure in DeleteAccountViewModel.");
        }
        finally
        {
            IsLocked = false;
        }
    }

    /// <summary> Guard to ensure only one deletion attempt is active at a time. </summary>
    private bool CanConfirm => !IsLocked;
}