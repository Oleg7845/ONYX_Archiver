using System.Windows.Controls;

namespace OnyxArchiver.UI.MVVM.Auth.Registration;

/// <summary>
/// Interaction logic for RegistrationView.xaml.
/// Provides the UI for new user onboarding.
/// This view captures essential account details and triggers the generation 
/// of the user's personal encryption keys.
/// </summary>
public partial class RegistrationView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegistrationView"/> class.
    /// </summary>
    public RegistrationView()
    {
        InitializeComponent();
    }
}