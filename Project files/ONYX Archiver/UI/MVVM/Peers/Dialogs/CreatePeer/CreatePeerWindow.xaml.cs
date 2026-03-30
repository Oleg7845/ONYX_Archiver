using System.Windows;

namespace OnyxArchiver.UI.MVVM.Peers.Dialogs.CreatePeer;

/// <summary>
/// Interaction logic for CreatePeerWindow.xaml.
/// This modal dialog initiates the peer creation process by allowing the user 
/// to define a new contact and generate an initial handshake file.
/// </summary>
public partial class CreatePeerWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreatePeerWindow"/> class.
    /// Expected to be bound to <see cref="CreatePeerViewModel"/>.
    /// </summary>
    public CreatePeerWindow()
    {
        InitializeComponent();
    }
}