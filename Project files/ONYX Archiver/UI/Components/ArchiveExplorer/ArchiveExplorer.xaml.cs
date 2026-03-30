using OnyxArchiver.Core.Models.Archive.VirtualCatalog;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OnyxArchiver.UI.Components;

/// <summary>
/// Interaction logic for the ArchiveExplorer component.
/// Provides a visual file explorer interface for navigating virtual archive structures.
/// Supports directory navigation, breadcrumbs via history, and multi-selection.
/// </summary>
public partial class ArchiveExplorer : UserControl
{
    // LIFO stack to track navigation history and support "Go Up" (backwards) functionality
    private readonly Stack<VirtualFolder> _history = new();

    /// <summary>
    /// Event triggered when the user initiates an extraction process for selected items.
    /// Passes a list of virtual paths to be processed by the ArchiveService.
    /// </summary>
    public event EventHandler<IEnumerable<string>>? SelectionChanged;

    public ArchiveExplorer()
    {
        InitializeComponent();
    }

    #region Dependency Properties

    /// <summary>
    /// The backing ViewModel that holds the current folder state and selection logic.
    /// </summary>
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(ArchiveExplorerViewModel),
            typeof(ArchiveExplorer),
            new PropertyMetadata(null, OnViewModelChanged));

    public ArchiveExplorerViewModel ViewModel
    {
        get => (ArchiveExplorerViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    /// <summary>
    /// Controls the visibility of the "Extract" action button.
    /// </summary>
    public static readonly DependencyProperty ShowExtractButtonProperty =
        DependencyProperty.Register(
            nameof(ShowExtractButton),
            typeof(bool),
            typeof(ArchiveExplorer),
            new PropertyMetadata(true));

    public bool ShowExtractButton
    {
        get => (bool)GetValue(ShowExtractButtonProperty);
        set => SetValue(ShowExtractButtonProperty, value);
    }

    /// <summary>
    /// The root directory of the archive. Setting this property resets the view to the archive top-level.
    /// </summary>
    public static readonly DependencyProperty RootProperty =
        DependencyProperty.Register(
            nameof(Root),
            typeof(VirtualFolder),
            typeof(ArchiveExplorer),
            new PropertyMetadata(null, OnRootChanged));

    public VirtualFolder Root
    {
        get => (VirtualFolder)GetValue(RootProperty);
        set => SetValue(RootProperty, value);
    }

    /// <summary>
    /// Command to be executed when the close/exit action is triggered within the component.
    /// </summary>
    public static readonly DependencyProperty CloseCommandProperty =
        DependencyProperty.Register(
            nameof(CloseCommand),
            typeof(ICommand),
            typeof(ArchiveExplorer),
            new PropertyMetadata(null));

    public ICommand CloseCommand
    {
        get => (ICommand)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(ArchiveExplorer),
            new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    #endregion

    #region Property Changed Callbacks

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ArchiveExplorer)d;
        // Sync the ViewModel's current directory with the Root if available
        if (control.ViewModel != null && control.Root != null)
        {
            control.ViewModel.CurrentFolder = control.Root;
        }
    }

    private static void OnRootChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ArchiveExplorer)d;
        var root = e.NewValue as VirtualFolder;

        if (root == null || control.ViewModel == null) return;

        // Reset the explorer to the new root folder
        control.ViewModel.CurrentFolder = root;
        control._history.Clear();
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles double-click events on grid items. Navigates into folders.
    /// </summary>
    private void OnFileDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FileGrid.SelectedItem is VirtualFolder folder)
        {
            // Push current state to history for back navigation
            _history.Push(ViewModel.CurrentFolder);
            ViewModel.CurrentFolder = folder;
        }
    }

    /// <summary>
    /// Synchronizes the DataGrid selection with the ViewModel's SelectedPaths list.
    /// Aggregates both files and folders into a unified path collection.
    /// </summary>
    private void OnFileSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel?.SelectedPaths == null) return;

        ViewModel.SelectedPaths.Clear();

        foreach (var item in FileGrid.SelectedItems)
        {
            string path = item is VirtualFile file
                ? file.Path
                : ((VirtualFolder)item).Path;

            ViewModel.SelectedPaths.Add(path);
        }

        ViewModel.TriggerSelectionChanged();
    }

    /// <summary>
    /// Navigates back to the previous folder in the navigation history.
    /// </summary>
    private void OnGoUpClick(object sender, RoutedEventArgs e)
    {
        if (_history.Count > 0)
        {
            ViewModel.CurrentFolder = _history.Pop();
        }
    }

    /// <summary>
    /// Signals the parent container to begin extraction of the currently selected paths.
    /// </summary>
    private void OnUnpackSelectedClick(object sender, RoutedEventArgs e)
    {
        SelectionChanged?.Invoke(this, ViewModel.SelectedPaths.ToList());
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        CloseCommand?.Execute(null);
    }

    #endregion
}

#region XAML Converters

/// <summary>
/// Resolves the appropriate PNG icon based on whether the item is a File or a Folder.
/// </summary>
public class TypeToIconConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        string iconName = value is VirtualFolder ? "folder.png" : "file.png";
        return $"/ONYX Archiver;component/UI/Resources/Icons/ArchiveTree/{iconName}";
    }
    public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) => null;
}

/// <summary>
/// Formats raw byte counts into human-readable strings (B, KB, MB, GB).
/// </summary>
public class FileSizeConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is long size)
        {
            string[] units = { "B", "KB", "MB", "GB" };
            double displaySize = size;
            int unitIndex = 0;

            while (displaySize >= 1024 && unitIndex < units.Length - 1)
            {
                displaySize /= 1024;
                unitIndex++;
            }
            return $"{displaySize:0.##} {units[unitIndex]}";
        }
        return "---"; // Folders typically don't show size in this view
    }
    public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) => null;
}

#endregion