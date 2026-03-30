using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OnyxArchiver.Domain.Exceptions;
using OnyxArchiver.Domain.Interfaces;
using OnyxArchiver.UI.Helpers;
using Serilog;

namespace OnyxArchiver.UI.MVVM.Settings.DialogWindows.ChangePassword;

/// <summary>
/// ViewModel for the password change dialog.
/// Manages identity verification via the current password and enforces 
/// complexity rules for the new credentials.
/// </summary>
public partial class ChangePasswordViewModel : ObservableObject
{
    private readonly IUserService _userService;

    /// <summary> Event to signal the View (Window) to close upon successful update. </summary>
    public event Action? RequestClose;

    /// <summary> The user's existing password, required for authorization. </summary>
    [ObservableProperty]
    private string _currentPassword = string.Empty;

    /// <summary> The new secret the user wishes to set. </summary>
    [ObservableProperty]
    private string _password = string.Empty;

    /// <summary> Verification field for the new password to prevent typos. </summary>
    [ObservableProperty]
    private string _confirmNewPassword = string.Empty;

    /// <summary> Prevents UI interaction and multiple submissions during the update. </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private bool _isLocked;

    /// <summary> UI-bound error message for validation or service failures. </summary>
    [ObservableProperty]
    private string _error = string.Empty;

    public ChangePasswordViewModel(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// CommunityToolkit hook: Validates the new password against policy in real-time.
    /// </summary>
    partial void OnPasswordChanged(string value)
    {
        // Uses the centralized PasswordValidator helper
        Error = PasswordValidator.GetValidationError(value) ?? string.Empty;
    }

    /// <summary>
    /// Executes the password update sequence.
    /// Validates inputs, matches passwords, and calls the user service for persistence.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private async Task ConfirmAsync()
    {
        Error = string.Empty;

        // 1. Basic UI-level validation
        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            Error = "Please enter your current password.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            Error = "Please enter a new password.";
            return;
        }

        if (!Password.Equals(ConfirmNewPassword))
        {
            Error = "The new passwords do not match.";
            return;
        }

        IsLocked = true;

        try
        {
            // 2. Service call: typically verifies the current password and 
            // re-encrypts the User Vault/Keys with the new password.
            await _userService.UpdateUserPasswordAsync(
                currentPassword: CurrentPassword,
                newPassword: Password);

            // 3. Success: Close the dialog
            RequestClose?.Invoke();
        }
        catch (UserNotFoundException ex)
        {
            Error = ex.UserMessage;
            Log.Warning("Password update failed: User not found.");
        }
        catch (InvalidPasswordException ex)
        {
            // Specifically handles the case where 'CurrentPassword' is wrong
            Error = "Current password is incorrect.";
            Log.Warning("Password update rejected: Invalid current password.");
        }
        catch (UserUpdatingException ex)
        {
            Error = ex.UserMessage;
            Log.Error(ex, "Service-side error during password update.");
        }
        catch (DomainException ex)
        {
            Error = ex.UserMessage;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Critical failure during password change.");
            Error = "An unexpected error occurred.";
        }
        finally
        {
            IsLocked = false;
        }
    }

    /// <summary> Command guard for the Confirm button. </summary>
    private bool CanConfirm => !IsLocked;
}