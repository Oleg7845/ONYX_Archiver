using System.Windows;

namespace OnyxArchiver.UI.MVVM.Peers.Dialogs.AcceptHandshake;

/// <summary>
/// Interaction logic for AcceptHandshakeWindow.xaml.
/// This modal dialog handles the second stage of the cryptographic handshake,
/// allowing the user to verify the incoming peer request and generate a response.
/// </summary>
public partial class AcceptHandshakeWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptHandshakeWindow"/> class.
    /// Expected to be initialized with <see cref="AcceptHandshakeViewModel"/>.
    /// </summary>
    public AcceptHandshakeWindow()
    {
        InitializeComponent();
    }
}