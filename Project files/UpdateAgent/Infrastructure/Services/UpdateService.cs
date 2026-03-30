using Serilog;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using UpdateAgent.Domain.Interfaces;

namespace UpdateAgent.Infrastructure.Services;

/// <summary>
/// Orchestrates the application update lifecycle including process monitoring, 
/// backup creation, file replacement, and failure recovery (rollback).
/// </summary>
public class UpdateService : IUpdateService
{
    private readonly IAppService _appService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateService"/>.
    /// </summary>
    /// <param name="appService">Service providing application paths and metadata.</param>
    public UpdateService(IAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// Main execution pipeline for the update process.
    /// Implements a "Try-Catch-Rollback" pattern to ensure system stability.
    /// </summary>
    /// <param name="progress">Progress reporter for UI granularity.</param>
    /// <param name="status">Status message reporter for user feedback.</param>
    public async Task RunUpdateAsync(
        IProgress<double>? progress = null,
        IProgress<string>? status = null)
    {
        // 1. Prerequisites: Ensure the target application is not locking any files.
        status?.Report("Waiting for app to close...");
        await WaitMainAppExit();

        // 2. Safety Net: Create a snapshot of the current installation.
        status?.Report("Creating backup...");
        string backupFolder = CreateBackup();

        try
        {
            // 3. Deployment: Extract new binaries.
            status?.Report("Extracting files...");
            await ExtractUpdate(progress);

            status?.Report("Finalizing...");

            // 4. Cleanup: Remove temporary update files and old backups upon success.
            if (File.Exists(_appService.UpdateFilePath))
                File.Delete(_appService.UpdateFilePath);

            // Note: In production, you might keep the backup longer, 
            // but here we clean up to save disk space after a confirmed success.
            Directory.Delete(backupFolder, true);

            progress?.Report(1.0);

            // 5. Completion: Relaunch the target application.
            RestartApplication();
        }
        catch (Exception ex)
        {
            // 6. Disaster Recovery: Revert to the previous state if any step fails.
            status?.Report("Error detected. Rolling back changes...");
            Log.Error(ex, "Update failed. Initiating rollback from: {BackupPath}", backupFolder);

            Rollback(backupFolder);
            throw; // Re-throw to allow UI to display the error state.
        }
    }

    /// <summary>
    /// Blocks execution until all instances of the target application are terminated.
    /// This prevents "File in Use" exceptions during extraction.
    /// </summary>
    private async Task WaitMainAppExit()
    {
        string processName = Path.GetFileNameWithoutExtension(_appService.AppExePath);

        // Polling approach to wait for process exit. 
        // Improvement note: Consider adding a timeout or a Force Kill option in future versions.
        while (Process.GetProcessesByName(processName).Any())
        {
            await Task.Delay(500);
        }
    }

    /// <summary>
    /// Copies all files from the application directory to a timestamped backup folder.
    /// </summary>
    /// <returns>The absolute path to the created backup directory.</returns>
    private string CreateBackup()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string backupFolder = Path.Combine(_appService.BackupFolderPath, $"pre_update_{timestamp}");

        Directory.CreateDirectory(backupFolder);

        // Shallow copy of the main directory. 
        // Note: For complex apps, this may need recursive sub-directory support.
        foreach (var file in Directory.GetFiles(_appService.AppFolderPath))
        {
            string fileName = Path.GetFileName(file);
            string dest = Path.Combine(backupFolder, fileName);
            File.Copy(file, dest, true);
        }

        return backupFolder;
    }

    /// <summary>
    /// Unpacks the ZIP archive into the target application folder.
    /// Uses <see cref="Task.Run"/> to offload heavy I/O and decompression from the UI thread.
    /// </summary>
    private async Task ExtractUpdate(IProgress<double>? progress)
    {
        await Task.Run(() =>
        {
            using var archive = ZipFile.OpenRead(_appService.UpdateFilePath);

            int total = archive.Entries.Count;
            int processed = 0;

            foreach (var entry in archive.Entries)
            {
                // Full path resolution ensuring sub-directories are handled.
                string destinationPath = Path.Combine(_appService.AppFolderPath, entry.FullName);

                // Ensure directory structure exists for the entry.
                string? directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                // Extract entry if it's a file (not a directory entry).
                if (!string.IsNullOrEmpty(entry.Name))
                {
                    entry.ExtractToFile(destinationPath, true);
                }

                processed++;
                progress?.Report((double)processed / total);
            }
        });
    }

    /// <summary>
    /// Restores files from the backup directory back to the application folder.
    /// Called only when an exception occurs during the extraction phase.
    /// </summary>
    private void Rollback(string backupFolder)
    {
        if (!Directory.Exists(backupFolder)) return;

        foreach (var file in Directory.GetFiles(backupFolder))
        {
            string fileName = Path.GetFileName(file);
            string dest = Path.Combine(_appService.AppFolderPath, fileName);
            File.Copy(file, dest, true);
        }
    }

    /// <summary>
    /// Launches the newly updated application and terminates the Update Agent process.
    /// </summary>
    private void RestartApplication()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = _appService.AppExePath,
            UseShellExecute = true // Required for some environments/permissions
        });

        Environment.Exit(0);
    }
}