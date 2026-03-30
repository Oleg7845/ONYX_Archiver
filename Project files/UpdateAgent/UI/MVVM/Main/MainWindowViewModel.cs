using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using UpdateAgent.Domain.Interfaces;

namespace UpdateAgent.UI.MVVM.Main;

/// <summary>
/// The ViewModel for the primary update window. 
/// Orchestrates the interaction between the <see cref="IUpdateService"/> and the UI, 
/// managing state updates and progress reporting.
/// </summary>
public partial class MainWindowViewModel : ObservableRecipient
{
    private readonly IAppService _appService;
    private readonly IUpdateService _updateService;

    /// <summary>
    /// The name of the application currently being updated.
    /// </summary>
    [ObservableProperty]
    private string? _appName;

    /// <summary>
    /// The formatted title displayed in the UI during the update process.
    /// </summary>
    [ObservableProperty]
    private string? _updatingProcessTitle;

    /// <summary>
    /// The current progress of the update operation, ranging from 0.0 to 1.0.
    /// </summary>
    [ObservableProperty]
    private double _progress;

    /// <summary>
    /// A human-readable string representing the current stage of the update (e.g., "Extracting...").
    /// </summary>
    [ObservableProperty]
    private string? _status;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/>.
    /// Automatically triggers the update sequence upon instantiation.
    /// </summary>
    /// <param name="appService">The provider for application metadata and paths.</param>
    /// <param name="updateService">The engine responsible for the update logic.</param>
    public MainWindowViewModel(IAppService appService, IUpdateService updateService)
    {
        _appService = appService;
        _updateService = updateService;

        // Enables the Messenger and other MVVM Toolkit features for this instance.
        IsActive = true;

        // Start the update process as an asynchronous fire-and-forget task.
        // We use an underscore (discard) to explicitly signal that we are not awaiting this task in the constructor.
        _ = UpdateAsync();
    }

    /// <summary>
    /// Encapsulates the high-level orchestration of the update process.
    /// Handles UI state transitions and captures errors for display and logging.
    /// </summary>
    private async Task UpdateAsync()
    {
        // Set initial UI state based on the provided app metadata.
        AppName = _appService.AppName;
        UpdatingProcessTitle = $"{AppName} updating...";
        Status = "Starting...";

        // Yield execution to allow the UI thread to complete its initialization 
        // and display the window before the heavy update logic begins.
        await Task.Yield();

        try
        {
            // Initiate the core update logic.
            // Progress and Status are updated via IProgress<T>, which automatically 
            // marshals calls back to the UI thread in WPF.
            await _updateService.RunUpdateAsync(
                new Progress<double>(p => Progress = p),
                new Progress<string>(s => Status = s));

            // Note: RestartApplication is typically called inside RunUpdateAsync on success.
        }
        catch (Exception ex)
        {
            // Graceful error handling for the user and detailed logging for the developer.
            Log.Error(ex, "An error occurred during the update of {AppName}", AppName);
            Status = "Update failed. Please check the logs.";
        }
    }
}