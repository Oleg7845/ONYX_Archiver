using System.Windows;

namespace UpdateAgent.UI.MVVM.Main;

/// <summary>
/// Interaction logic for MainWindow.xaml.
/// This class acts as the primary View in the MVVM pattern, responsible only for 
/// UI initialization and binding the ViewModel to the DataContext.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/>.
    /// </summary>
    /// <param name="mainWindowViewModel">
    /// The ViewModel instance provided by the Dependency Injection container.
    /// This ensures a clean separation of concerns between UI and business logic.
    /// </param>
    public MainWindow(MainWindowViewModel mainWindowViewModel)
    {
        // Standard WPF component initialization (parsing XAML).
        InitializeComponent();

        // Assigning the injected ViewModel as the data source for XAML bindings.
        DataContext = mainWindowViewModel;
    }
}