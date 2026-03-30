using OnyxArchiver.Core.Models.Archive;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace OnyxArchiver.UI.MVVM.Archive.Open;

/// <summary>
/// Interaction logic for OpenArchiveView.xaml.
/// Handles archive exploration and drag-and-drop opening of .onx files.
/// </summary>
public partial class OpenArchiveView : UserControl
{
    public OpenArchiveView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Reacts to user selection within the archive file tree.
    /// Switches between full unpacking and selective extraction of specific files.
    /// </summary>
    private void ArchiveExplorer_SelectionChanged(object sender, IEnumerable<string> paths)
    {
        if (DataContext is OpenArchiveViewModel vm)
        {
            // If nothing is selected, the default action is to unpack the entire archive
            if (paths.Count() == 0)
            {
                vm.FullUnpackingCommand.ExecuteAsync(null);
            }
            else
            {
                // Otherwise, pass the list of selected internal paths for partial extraction
                vm.SelectiveUnpackingCommand.ExecuteAsync(paths);
            }
        }
    }

    /// <summary>
    /// Standard WPF DragOver implementation to allow file dropping.
    /// </summary>
    private void DropArea_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    /// <summary>
    /// Handles the Drop event. Validates that the dropped file is a valid Onyx archive 
    /// by checking its extension against the ArchiveHeader.Magic constant.
    /// </summary>
    private void DropArea_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (string path in paths)
            {
                if (File.Exists(path))
                {
                    // Validation: Only accept files with the correct extension (e.g., .onx)
                    string extension = new FileInfo(path).Extension.ToLower().Replace(".", "");

                    if (extension != ArchiveHeader.Magic.ToLower())
                        continue; // Skip invalid files

                    if (DataContext is OpenArchiveViewModel vm)
                    {
                        vm.SetArchivePathCommand.ExecuteAsync(path);
                    }

                    // Process only the first valid archive found in the drop
                    return;
                }
            }
        }
    }
}