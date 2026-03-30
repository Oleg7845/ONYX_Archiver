using System.Windows.Controls;

namespace OnyxArchiver.UI.MVVM.Settings;

/// <summary>
/// Interaction logic for SettingsView.xaml.
/// Provides the user interface for application-wide configurations, 
/// including security preferences and directory defaults.
/// </summary>
public partial class SettingsView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsView"/> class.
    /// </summary>
    public SettingsView()
    {
        InitializeComponent();
    }
}