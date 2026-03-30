using System.Windows.Controls;

namespace OnyxArchiver.UI.MVVM.Auth.Login;

/// <summary>
/// Interaction logic for LoginView.xaml.
/// Provides the user interface for identity verification.
/// This view typically contains fields for username/email and a password, 
/// which are used to unlock the user's local security credentials.
/// </summary>
public partial class LoginView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginView"/> class.
    /// </summary>
    public LoginView()
    {
        InitializeComponent();
    }
}