using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace OnyxArchiver.UI.MVVM.Archive.Add;

/// <summary>
/// Interaction logic for AddArchiveView.xaml.
/// Implements Drag-and-Drop functionality to allow users to easily provide 
/// source files or directories for the archiving process.
/// </summary>
public partial class AddArchiveView : UserControl
{
    public AddArchiveView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Event handler for the DragOver event. 
    /// Checks if the dragged data contains files and sets the cursor effect accordingly.
    /// </summary>
    private void DropArea_DragOver(object sender, DragEventArgs e)
    {
        // Check if the data format is a file drop (standard Windows Explorer drop)
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        // Mark event as handled to prevent standard routing
        e.Handled = true;
    }

    /// <summary>
    /// Event handler for the Drop event. 
    /// Extracts paths from the dropped data and pushes them to the ViewModel.
    /// </summary>
    private void DropArea_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            // Retrieve an array of full paths for the dropped items
            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);

            // Iterate through paths and find the first valid directory or file
            foreach (string path in paths)
            {
                if (Directory.Exists(path) || File.Exists(path))
                {
                    // Cast DataContext to the specific ViewModel to execute the command
                    if (DataContext is AddArchiveViewModel vm)
                    {
                        // Note: Using ExecuteAsync as Task-based commands are common in MVVM Toolkit
                        vm.SetSourcePathCommand.ExecuteAsync(path);
                    }

                    // Break or return after the first valid path if the UI supports single-item addition
                    return;
                }
            }
        }
    }
}