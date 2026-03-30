using OnyxArchiver.UI.Models;
using OnyxArchiver.Domain.Interfaces;
using OnyxArchiver.Infrastructure.DTOs;
using System.Windows;
using System.Windows.Controls;
using OnyxArchiver.UI.MVVM.Main.Progress;

namespace OnyxArchiver.UI.MVVM.Main.Dialogs;

/// <summary>
/// Interaction logic for UpdateDialog.xaml.
/// Provides the user interface for notifying the user about a new version 
/// and handling the update initiation process.
/// </summary>
public partial class UpdateDialog : UserControl
{
    private readonly IDialogService _dialogService;
    private readonly IUpdateService _updateService;

    /// <summary> Gets or sets the data containing information about the available update. </summary>
    public UpdateCheckResponse? Update { get; set; }

    /// <summary> Gets or sets the progress reporter to track the download status. </summary>
    public Progress<ProgressReport>? Progress { get; set; }

    /// <summary> Callback triggered when the user confirms the update and the process starts. </summary>
    public Action? OnUpdateStarted { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateDialog"/> class with required services.
    /// </summary>
    public UpdateDialog(IDialogService dialogService, IUpdateService updateService)
    {
        InitializeComponent();
        _dialogService = dialogService;
        _updateService = updateService;
    }

    /// <summary>
    /// Handles the 'Later' button click. Simply closes the current modal overlay.
    /// </summary>
    private void LaterClick(object sender, RoutedEventArgs e)
    {
        _dialogService.Close();
    }

    /// <summary>
    /// Handles the 'Update' button click. 
    /// Closes the prompt, triggers the progress UI, and starts the asynchronous download/install.
    /// </summary>
    private async void UpdateClick(object sender, RoutedEventArgs e)
    {
        if (Update == null || Progress == null) return;

        // Close the invitation dialog first
        _dialogService.Close();

        // Notify the parent (usually MainViewModel) to show the ProgressWindow
        OnUpdateStarted?.Invoke();

        try
        {
            // Execute the update logic provided by the infrastructure layer
            await _updateService.UpdateAsync(Update, Progress);
        }
        catch (Exception ex)
        {
            // In a production app, you might want to show an error message here
            System.Diagnostics.Debug.WriteLine($"Update failed: {ex.Message}");
        }
        finally
        {
            // Ensure all progress windows are closed once the operation is finished or fails
            foreach (Window window in Application.Current.Windows)
            {
                if (window is ProgressWindow)
                {
                    window.Close();
                }
            }
        }
    }
}