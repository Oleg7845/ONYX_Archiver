using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OnyxArchiver.Domain.Exceptions;
using OnyxArchiver.Domain.Interfaces;
using OnyxArchiver.UI.Abstractions;
using OnyxArchiver.UI.Messages;
using Serilog;

namespace OnyxArchiver.UI.MVVM.Auth.Login;

/// <summary>
/// ViewModel responsible for handling user authentication.
/// Orchestrates the transition from the login screen to the main application functional areas.
/// </summary>
public partial class LoginViewModel : ObservableObject, IFullScreenPage
{
    private readonly IAuthService _authService;

    /// <summary> The unique identifier or email used for logging in. </summary>
    [ObservableProperty]
    private string _username = string.Empty;

    /// <summary> 
    /// The user's password. 
    /// Note: In a production WPF app, consider passing SecureString from the View 
    /// via CommandParameter to avoid keeping plain text in memory. 
    /// </summary>
    [ObservableProperty]
    private string _password = string.Empty;

    /// <summary> 
    /// Prevents multiple simultaneous login attempts and disables UI controls during processing. 
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private bool _isLocked;

    /// <summary> Validation or server-side error message displayed to the user. </summary>
    [ObservableProperty]
    private string _error = string.Empty;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Executes the core authentication logic.
    /// Redirects to the Archive creation page upon successful validation.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        Error = string.Empty;

        // 1. Client-side validation
        if (string.IsNullOrWhiteSpace(Username))
        {
            Error = "Please enter your username.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            Error = "Please enter your password.";
            return;
        }

        IsLocked = true;

        try
        {
            // 2. Call to Domain/Infrastructure layer
            await _authService.Login(Username, Password);

            // 3. Cleanup sensitive data from the VM state
            Username = string.Empty;
            Password = string.Empty;

            // 4. Broadcast navigation event to the Shell/Main Window
            WeakReferenceMessenger.Default.Send(new NavigationMessage(NavigationPage.AddArchive));
        }
        catch (UserNotFoundException ex)
        {
            Error = ex.UserMessage; // Decoupled message from domain exception
            Log.Warning(ex, "Failed login attempt: User not found.");
        }
        catch (InvalidPasswordException ex)
        {
            Error = ex.UserMessage;
            Log.Warning(ex, "Failed login attempt: Invalid password.");
        }
        catch (DomainException ex)
        {
            Error = ex.UserMessage;
            Log.Error(ex, "Domain error during login process.");
        }
        catch (Exception ex)
        {
            Error = "A critical system error occurred. Please try again later.";
            Log.Fatal(ex, "Unexpected crash in LoginViewModel.");
        }
        finally
        {
            IsLocked = false;
        }
    }

    /// <summary> Returns true if the UI is not currently processing a request. </summary>
    private bool CanLogin => !IsLocked;

    /// <summary>
    /// Switches the UI context to the Registration view.
    /// </summary>
    [RelayCommand]
    private void GoToRegistration() =>
        WeakReferenceMessenger.Default.Send(new NavigationMessage(NavigationPage.Registration));
}