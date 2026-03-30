using System.Windows;

namespace OnyxArchiver.UI.MVVM.Peers.Dialogs.UpdatePeer;

/// <summary>
/// Interaction logic for UpdatePeerWindow.xaml.
/// This modal dialog allows users to modify the metadata of an existing trusted peer,
/// such as their display name or notes, without altering the established cryptographic keys.
/// </summary>
public partial class UpdatePeerWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdatePeerWindow"/> class.
    /// Typically launched from the <see cref="PeersViewModel"/> using the Edit command.
    /// </summary>
    public UpdatePeerWindow()
    {
        InitializeComponent();
    }
}