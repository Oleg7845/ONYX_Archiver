using CryptoCore.Cryptography.Hashing;
using CryptoCore.Cryptography.Keys.Ed25519;
using CryptoCore.Cryptography.Signing;
using OnyxArchiver.Domain.Interfaces;
using OnyxArchiver.Infrastructure.DTOs;
using OnyxArchiver.UI.Models;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OnyxArchiver.Infrastructure.Services;

/// <summary>
/// Manages application updates, including checking for new versions, secure downloading,
/// cryptographic verification, and launching the external Update Agent.
/// </summary>
public class UpdateService : IUpdateService
{
    private readonly IAppService _appService;
    private readonly HttpClient _http;

    public UpdateService(IAppService appService, HttpClient http)
    {
        _appService = appService;
        _http = http;
    }

    /// <summary>
    /// Checks the remote server for available updates based on current app version and channel.
    /// </summary>
    /// <returns>Metadata about the new version or null if no update is available or server is unreachable.</returns>
    public async Task<UpdateCheckResponse?> CheckAsync()
    {
        var url = $"{_appService.ServerUrl}/api/update/check?app={_appService.AppName}&version={_appService.AppVersion}&channel=stable";

        try
        {
            using var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UpdateCheckResponse>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Downloads the update package with real-time progress reporting.
    /// </summary>
    private async Task DownloadAsync(
        UpdateCheckResponse update,
        IProgress<ProgressReport>? progress = null)
    {
        var url = _appService.ServerUrl + update.Url;

        // Use ResponseHeadersRead to start streaming immediately and calculate progress
        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? update.Size;

        using var stream = await response.Content.ReadAsStreamAsync();
        using var file = File.Create(_appService.UpdateFilePath);

        var buffer = new byte[65536]; // 64KB buffer for balanced I/O performance
        long totalRead = 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
            if (bytesRead == 0) break;

            await file.WriteAsync(buffer.AsMemory(0, bytesRead));
            totalRead += bytesRead;

            if (totalBytes > 0 && progress != null)
            {
                // Calculate transfer metrics: MBs, Speed (Mbps), and Estimated Time Remaining
                double elapsedSeconds = sw.Elapsed.TotalSeconds;
                double bytesPerSecond = elapsedSeconds > 0 ? totalRead / elapsedSeconds : 0;
                TimeSpan remaining = bytesPerSecond > 0
                    ? TimeSpan.FromSeconds((totalBytes - totalRead) / bytesPerSecond)
                    : TimeSpan.Zero;

                progress.Report(new ProgressReport
                {
                    Percentage = (double)totalRead / totalBytes * 100,
                    DownloadedMb = (double)totalRead / 1048576,
                    TotalMb = (double)totalBytes / 1048576,
                    RemainingTime = remaining,
                    SpeedMbps = (bytesPerSecond * 8) / 1000000
                });
            }
        }
        sw.Stop();
    }

    /// <summary>
    /// Compares the downloaded file hash against the server-provided hash using constant-time comparison.
    /// </summary>
    private bool VerifyHash(string fileHash, string expectedHash)
    {
        // Use FixedTimeEquals to prevent timing attacks during validation
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(fileHash),
            Convert.FromHexString(expectedHash));
    }

    /// <summary>
    /// Validates the Ed25519 digital signature of the update package.
    /// Ensures the file was signed by the official server's private key.
    /// </summary>
    private bool VerifySignature(string fileHash, string signature)
    {
        using Ed25519KeyContext ed25519KeyContext = new Ed25519KeyContext();
        using Ed25519Provider ed25519Provider = new Ed25519Provider(ed25519KeyContext);

        return ed25519Provider.VerifyRemote(
            Encoding.UTF8.GetBytes(fileHash),
            Convert.FromBase64String(signature),
            Convert.FromBase64String(_appService.ServerSignaturePublicKey));
    }

    /// <summary>
    /// Transfers control to the external Update Agent and terminates the current process.
    /// Requires elevated privileges ("runas") to overwrite files in Protected Folders.
    /// </summary>
    private void LaunchUpdater()
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = _appService.UpdateAgentExePath,
            // Pass current app name, main EXE path, and the downloaded zip to the agent
            Arguments = $"\"{_appService.AppName}\" \"{_appService.ExePath}\" \"{_appService.UpdateFilePath}\"",
            UseShellExecute = true,
            Verb = "runas"
        };

        System.Diagnostics.Process.Start(psi);
        Environment.Exit(0);
    }

    /// <summary>
    /// Orchestrates the full update sequence: Download -> Integrity Check -> Authenticity Check -> Launch.
    /// </summary>
    public async Task UpdateAsync(
        UpdateCheckResponse update,
        IProgress<ProgressReport>? progress = null)
    {
        await DownloadAsync(update, progress);

        if (!File.Exists(_appService.UpdateFilePath))
            throw new FileNotFoundException("The update package was not found after download.");

        // 1. Integrity Check (Is the file corrupted?)
        string fileHash = FileToSha512.HashFile(_appService.UpdateFilePath);
        if (!VerifyHash(fileHash, update.Hash!))
        {
            CleanupUpdateFile();
            throw new CryptographicException("Hash mismatch: The downloaded file is corrupted or modified.");
        }

        // 2. Authenticity Check (Is this an official update?)
        if (!FileSignatureService.VerifyFile(_appService.UpdateFilePath, update.Signature!, _appService.ServerSignaturePublicKey))
        {
            CleanupUpdateFile();
            throw new CryptographicException("Signature verification failed: Package origin cannot be trusted.");
        }

        LaunchUpdater();
    }

    private void CleanupUpdateFile()
    {
        if (File.Exists(_appService.UpdateFilePath))
            File.Delete(_appService.UpdateFilePath);
    }
}