using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OnyxArchiver.Domain.Exceptions;
using OnyxArchiver.Domain.Interfaces;
using OnyxArchiver.UI.Abstractions;
using OnyxArchiver.UI.Helpers;
using OnyxArchiver.UI.Messages;
using Serilog;

namespace OnyxArchiver.UI.MVVM.Auth.Registration;

/// <summary>
/// ViewModel responsible for the user registration process.
/// Handles credential validation, password complexity checks, and account initialization.
/// </summary>
public partial class RegistrationViewModel : ObservableObject, IFullScreenPage
{
    private readonly IAuthService _authService;

    /// <summary> The unique identifier chosen by the new user. </summary>
    [ObservableProperty]
    private string _username = string.Empty;

    /// <summary> The primary secret for account access. </summary>
    [ObservableProperty]
    private string _password = string.Empty;

    /// <summary> Verification field to prevent accidental typos in the password. </summary>
    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    /// <summary> UI lock state to prevent re-entry during the cryptographic key generation. </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegistrationCommand))]
    private bool _isLocked;

    /// <summary> Current validation or system error to be displayed in the UI. </summary>
    [ObservableProperty]
    private string _error = string.Empty;

    public RegistrationViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// CommunityToolkit hook: triggered automatically when the Password property is updated.
    /// Provides immediate visual feedback on password strength or requirements.
    /// </summary>
    partial void OnPasswordChanged(string value)
    {
        // PasswordValidator is a helper that checks length, symbols, etc.
        Error = PasswordValidator.GetValidationError(value) ?? string.Empty;
    }

    /// <summary>
    /// Processes the registration request.
    /// On success, redirects the user to the login screen.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRegistration))]
    private async Task RegistrationAsync()
    {
        Error = string.Empty;

        // 1. Basic presence checks
        if (string.IsNullOrWhiteSpace(Username))
        {
            Error = "Please choose a username.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            Error = "Please set a password.";
            return;
        }

        if (!Password.Equals(ConfirmPassword))
        {
            Error = "Passwords do not match.";
            return;
        }

        IsLocked = true;

        try
        {
            // 2. Call the domain service (which likely generates RSA/AES keys here)
            await _authService.Registration(Username, Password);

            // 3. Clear sensitive data before leaving the page
            Username = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;

            // 4. Navigate back to Login so the user can sign in with their new keys
            WeakReferenceMessenger.Default.Send(new NavigationMessage(NavigationPage.Login));
        }
        catch (UserAlreadyExistsException ex)
        {
            Error = ex.UserMessage;
            Log.Warning("Registration failed: Username '{User}' is taken.", Username);
        }
        catch (DomainException ex)
        {
            Error = ex.UserMessage;
            Log.Error(ex, "Domain error during registration.");
        }
        catch (Exception ex)
        {
            Error = "An unexpected error occurred during account creation.";
            Log.Fatal(ex, "Critical failure in RegistrationViewModel.");
        }
        finally
        {
            IsLocked = false;
        }
    }

    /// <summary> Command guard based on the lock state. </summary>
    private bool CanRegistration => !IsLocked;

    /// <summary>
    /// Returns the user to the Login screen if they already have an account.
    /// </summary>
    [RelayCommand]
    private void BackToLogin() =>
        WeakReferenceMessenger.Default.Send(new NavigationMessage(NavigationPage.Login));
}