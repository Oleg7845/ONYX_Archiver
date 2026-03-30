using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using OnyxArchiver.UI.Models;
using OnyxArchiver.Domain.Interfaces;
using OnyxArchiver.Infrastructure.DTOs;
using OnyxArchiver.UI.Abstractions;
using OnyxArchiver.UI.Messages;
using OnyxArchiver.UI.MVVM.Archive.Add;
using OnyxArchiver.UI.MVVM.Archive.Open;
using OnyxArchiver.UI.MVVM.Auth.Login;
using OnyxArchiver.UI.MVVM.Auth.Registration;
using OnyxArchiver.UI.MVVM.Main.Dialogs;
using OnyxArchiver.UI.MVVM.Main.Progress;
using OnyxArchiver.UI.MVVM.Peers;
using OnyxArchiver.UI.MVVM.Settings;

namespace OnyxArchiver.UI.MVVM.Main;

/// <summary>
/// The shell ViewModel of the application. 
/// Acts as a central coordinator for navigation, state management, and view switching using the ServiceProvider.
/// </summary>
public partial class MainViewModel
    : ObservableRecipient,
    IRecipient<NavigationMessage>,
    IRecipient<ArchivePathMessage>,
    IRecipient<HandshakePathMessage>,
    IRecipient<PathMessage>
{
    private readonly IAppService _appService;
    private readonly IUpdateService _updateService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuthService _authService;
    private readonly IDialogService _dialogService;

    /// <summary> Gets or sets the ViewModel currently displayed in the main ContentControl. </summary>
    [ObservableProperty]
    private ObservableObject? _currentView;

    /// <summary> Gets or sets a value indicating whether the side navigation menu should be visible (e.g., hidden on Login/Registration). </summary>
    [ObservableProperty]
    private bool _isNavigationVisible;

    /// <summary> The formatted application version string. </summary>
    [ObservableProperty]
    private string? _appVersion;

    /// <summary> Current progress percentage of the update downloading operation (0-100). </summary>
    [ObservableProperty]
    private double _updateDownloadingProgress;

    /// <summary> Status message describing the current background operation or download state. </summary>
    [ObservableProperty]
    private string _currentStatusText;

    /// <summary>
    /// Initializes a new instance of the MainViewModel with required core services.
    /// </summary>
    public MainViewModel(
        IAppService appService,
        IUpdateService updateService,
        IServiceProvider serviceProvider,
        IAuthService authService,
        IDialogService dialogService)
    {
        _appService = appService;
        _updateService = updateService;
        _serviceProvider = serviceProvider;
        _authService = authService;
        _dialogService = dialogService;

        AppVersion = $"Version: {_appService.AppVersion}";

        // Fire-and-forget update check on startup
        _ = RunUpdateChack();

        // Enable Messenger to start receiving cross-component messages
        IsActive = true;

        // Initial routing based on auth state
        NavigateByRole();
    }

    /// <summary>
    /// Initial navigation logic based on the user's authentication status.
    /// Redirects to AddArchive if logged in, otherwise to the Login page.
    /// </summary>
    private void NavigateByRole()
    {
        if (_authService.IsLoggedIn)
            NavigateTo<AddArchiveViewModel>();
        else
            NavigateTo<LoginViewModel>();
    }

    /// <summary>
    /// Handles NavigationMessage to switch between views from anywhere in the application.
    /// </summary>
    /// <param name="message">The message containing the target NavigationPage enum.</param>
    public void Receive(NavigationMessage message)
    {
        switch (message.Page)
        {
            case NavigationPage.Login: NavigateTo<LoginViewModel>(); break;
            case NavigationPage.Registration: NavigateTo<RegistrationViewModel>(); break;
            case NavigationPage.AddArchive: NavigateTo<AddArchiveViewModel>(); break;
            case NavigationPage.OpenArchive: NavigateTo<OpenArchiveViewModel>(); break;
            case NavigationPage.Peers: NavigateTo<PeersViewModel>(); break;
            case NavigationPage.Settings: NavigateTo<SettingsViewModel>(); break;
        }
    }

    /// <summary>
    /// Core navigation method. Resolves the requested ViewModel from the DI container 
    /// and manages ObservableRecipient activation lifecycles to prevent memory leaks.
    /// </summary>
    /// <typeparam name="T">The type of the ViewModel to navigate to.</typeparam>
    private void NavigateTo<T>() where T : ObservableObject
    {
        // Deactivate old ViewModel to stop its message subscriptions
        if (CurrentView is ObservableRecipient oldVm)
        {
            oldVm.IsActive = false;
        }

        // Resolve the new ViewModel instance from the Service Provider
        var viewModel = _serviceProvider.GetRequiredService<T>();

        CurrentView = viewModel;

        // Activate new ViewModel if it needs to receive messages
        if (viewModel is ObservableRecipient newVm)
        {
            newVm.IsActive = true;
        }

        // Sidebar is only visible for authenticated users on non-fullscreen pages
        IsNavigationVisible = _authService.IsLoggedIn && viewModel is not IFullScreenPage;
    }

    /// <summary>
    /// Navigates to the Archive creation view. 
    /// If already on the view, triggers a native file selection dialog to add new sources.
    /// </summary>
    [RelayCommand]
    private void ShowAddArchive()
    {
        if (CurrentView is AddArchiveViewModel openVm)
        {
            string dialogFileName = "Select file or folder";

            var dialog = new OpenFileDialog
            {
                Title = "Select Archive source",
                Filter = "All files (*.*)|*.*",
                CheckFileExists = false, // Allows folder selection logic
                CheckPathExists = true,
                FileName = dialogFileName
            };

            if (dialog.ShowDialog() == true)
            {
                // Clean the filename to handle folder-selection-via-dialog workaround
                openVm.Receive(
                    new PathMessage(
                        dialog.FileName.Replace(dialogFileName, string.Empty).TrimEnd('\\', '/')));
            }
        }
        else
        {
            NavigateTo<AddArchiveViewModel>();
        }
    }

    /// <summary>
    /// Navigates to the Archive extraction view. 
    /// If already on the view, triggers a file dialog to pick an .onx archive.
    /// </summary>
    [RelayCommand]
    private void ShowOpenArchive()
    {
        if (CurrentView is OpenArchiveViewModel archiveVm)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select archive file",
                Filter = "Onyx Archive (*.onx)|*.onx",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                archiveVm.Receive(new ArchivePathMessage(dialog.FileName));
            }
        }
        else
        {
            NavigateTo<OpenArchiveViewModel>();
        }
    }

    /// <summary> Command to switch to the Peer management screen. </summary>
    [RelayCommand]
    private void ShowPeers() => NavigateTo<PeersViewModel>();

    /// <summary> Command to switch to the Settings screen. </summary>
    [RelayCommand]
    private void ShowSettings() => NavigateTo<SettingsViewModel>();

    /// <summary> Logs out the current user, clears auth state, and redirects to the Login screen. </summary>
    [RelayCommand]
    private async Task Logout()
    {
        await _authService.Logout();
        NavigateTo<LoginViewModel>();
    }

    /// <summary> Handles external requests to open an archive (e.g., from file association). </summary>
    public void Receive(ArchivePathMessage message)
    {
        if (!_authService.IsLoggedIn) { NavigateTo<LoginViewModel>(); return; }

        if (CurrentView is not OpenArchiveViewModel) NavigateTo<OpenArchiveViewModel>();
        (CurrentView as OpenArchiveViewModel)?.Receive(message);
    }

    /// <summary> Handles external handshake file paths (e.g., drag and drop or CLI). </summary>
    public void Receive(HandshakePathMessage message)
    {
        if (!_authService.IsLoggedIn) { NavigateTo<LoginViewModel>(); return; }

        if (CurrentView is not PeersViewModel) NavigateTo<PeersViewModel>();
        (CurrentView as PeersViewModel)?.Receive(message);
    }

    /// <summary> Handles generic file path messages for adding items to a new archive. </summary>
    public void Receive(PathMessage message)
    {
        if (!_authService.IsLoggedIn) { NavigateTo<LoginViewModel>(); return; }

        if (CurrentView is not AddArchiveViewModel) NavigateTo<AddArchiveViewModel>();
        (CurrentView as AddArchiveViewModel)?.Receive(message);
    }

    /// <summary>
    /// Checks for application updates and displays a modal dialog with a progress indicator if found.
    /// </summary>
    private async Task RunUpdateChack()
    {
        UpdateCheckResponse? update = await _updateService.CheckAsync();

        if (update?.HasUpdate == true)
        {
            // Define how progress reports from the downloader should update the UI
            var progressIndicator = new Progress<ProgressReport>(report => {
                UpdateDownloadingProgress = report.Percentage;
                CurrentStatusText = string.Format(
                    "{0:F1} MB / {1:F1} MB (Remaining: {2:mm\\:ss})",
                    report.DownloadedMb,
                    report.TotalMb,
                    report.RemainingTime);
            });

            // Show modal update dialog via the DialogService
            _dialogService.Show<UpdateDialog>(updateDialog =>
            {
                updateDialog.Update = update;
                updateDialog.Progress = progressIndicator;
                updateDialog.OnUpdateStarted = () =>
                {
                    // Open a non-blocking progress window to track the download
                    var progressWindow = new ProgressWindow
                    {
                        DataContext = this,
                        Owner = System.Windows.Application.Current.MainWindow
                    };
                    progressWindow.Show();
                };
            });
        }
    }
}