using OnyxArchiver.UI.MVVM.Main.Overlay;
using System.Windows.Controls;

namespace OnyxArchiver.Domain.Interfaces;

/// <summary>
/// Orchestrates the display of modal overlays and dialog windows within the application.
/// Decouples the ViewModels from the specific UI implementation of pop-ups.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Attaches the service to a specific UI container (OverlayHost) that will render the dialogs.
    /// Typically called during the main window initialization.
    /// </summary>
    /// <param name="host">The UI element responsible for hosting the overlay content.</param>
    void Initialize(OverlayHost host);

    /// <summary>
    /// Displays a modal dialog containing the specified View/UserControl.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="UserControl"/> to be displayed in the overlay.</typeparam>
    /// <param name="configure">An optional callback to initialize the View or its ViewModel before display.</param>
    /// <remarks>
    /// This is used for workflows like "Add Peer," "Enter Password," or "Update Settings."
    /// </remarks>
    void Show<T>(Action<T>? configure = null) where T : UserControl;

    /// <summary>
    /// Closes the currently active dialog and returns focus to the main application layer.
    /// </summary>
    void Close();
}