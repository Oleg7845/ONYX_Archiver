using OnyxArchiver.Domain.Interfaces;
using System.Windows;

namespace OnyxArchiver.UI.MVVM.Main;

/// <summary>
/// The primary window of the Onyx Archiver application.
/// Manages the top-level visual shell and initializes the global overlay system.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes the main window with its corresponding ViewModel and services.
    /// </summary>
    /// <param name="viewModel">The root logic controller for navigation and app state.</param>
    /// <param name="dialogService">The service responsible for managing in-app overlays and messages.</param>
    public MainWindow(MainViewModel viewModel, IDialogService dialogService)
    {
        InitializeComponent();

        // Root binding for the Entire UI
        DataContext = viewModel;

        // Binds the DialogService to a specific UI container (likely a Grid or Border named 'Overlay')
        // to render non-blocking and modal notifications within the main window context.
        dialogService.Initialize(Overlay);
    }
}