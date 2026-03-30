using System.Windows;
using System.Windows.Controls;

namespace OnyxArchiver.UI.MVVM.Main.Overlay;

/// <summary>
/// Interaction logic for OverlayHost.xaml.
/// Acts as a global container for in-app modal dialogs, alerts, and custom overlays,
/// allowing them to be displayed on top of the main UI without opening separate Windows.
/// </summary>
public partial class OverlayHost : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OverlayHost"/> class.
    /// </summary>
    public OverlayHost()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Injects a UI element into the overlay container and makes it visible.
    /// Typically called by the <see cref="IDialogService"/> to show a specific View.
    /// </summary>
    /// <param name="dialog">The UI element (e.g., a UserControl) to be displayed as a modal.</param>
    public void Show(UIElement dialog)
    {
        // Set the dynamic content of the overlay
        DialogContent.Content = dialog;

        // Make the entire overlay layer visible (usually involves a semi-transparent background)
        OverlayRoot.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Clears the current dialog content and hides the overlay layer.
    /// </summary>
    public void Hide()
    {
        // Remove the reference to the dialog to allow for garbage collection
        DialogContent.Content = null;

        // Collapse the overlay layer to return focus to the main application content
        OverlayRoot.Visibility = Visibility.Collapsed;
    }
}