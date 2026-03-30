using OnyxArchiver.Domain.Interfaces;
using OnyxArchiver.UI;
using OnyxArchiver.UI.MVVM.Main.Overlay;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace OnyxArchiver.Infrastructure.Services;

/// <summary>
/// Service for managing UI overlays and modal dialogs.
/// Decouples View activation from ViewModels by using the Dependency Injection container.
/// </summary>
public class DialogService : IDialogService
{
    // A reference to the UI element that acts as a container for overlays.
    private OverlayHost? _host;

    /// <summary>
    /// Connects the service to the UI overlay host. 
    /// Typically called once during the Main Window's initialization.
    /// </summary>
    /// <param name="host">The visual element responsible for rendering dialogs.</param>
    public void Initialize(OverlayHost host)
    {
        _host = host;
    }

    /// <summary>
    /// Resolves a dialog of type <typeparamref name="T"/> from the DI container and displays it.
    /// </summary>
    /// <typeparam name="T">The type of UserControl to be displayed as a dialog.</typeparam>
    /// <param name="configure">Optional callback to initialize or inject data into the dialog instance before display.</param>
    public void Show<T>(Action<T>? configure = null) where T : UserControl
    {
        // Resolve the view from the service provider to support constructor injection in views/viewmodels
        var dialog = App.Services!.GetRequiredService<T>();

        // Allow external configuration (e.g., setting a specific ViewModel property)
        configure?.Invoke(dialog);

        // Ensure the UI update is performed on the Main UI Thread
        Application.Current.Dispatcher.Invoke(() =>
        {
            _host?.Show(dialog);
        });
    }

    /// <summary>
    /// Closes the currently active overlay and clears the host area.
    /// </summary>
    public void Close()
    {
        // Thread-safe call to hide the overlay host
        Application.Current.Dispatcher.Invoke(() =>
        {
            _host?.Hide();
        });
    }
}