using OnyxArchiver.Domain.Exceptions;
using OnyxArchiver.Domain.Interfaces;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace OnyxArchiver.Infrastructure.Services.Security;

/// <summary>
/// Provides a high-level API for securely persisting sensitive data (keys, tokens) 
/// using the Windows Data Protection API (DPAPI).
/// </summary>
/// <remarks>
/// Encrypted data is bound to the current OS user profile (<see cref="DataProtectionScope.CurrentUser"/>),
/// preventing decryption by other users on the same machine or on different devices.
/// </remarks>
public class SecureStorageService : ISecureStorageService
{
    /// <summary> 
    /// Additional entropy used to increase encryption complexity. 
    /// Prevents other applications from easily decrypting Onyx-specific data using standard DPAPI calls.
    /// </summary>
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Onyx-Archiver-Secret-Salt");

    /// <summary>
    /// Resolves a secure file path within the user's Local AppData directory.
    /// This ensures the application can read/write data without requiring administrative privileges.
    /// </summary>
    /// <param name="fileName">The intended name or relative path of the file.</param>
    /// <returns>The absolute path within the application's local data folder.</returns>
    private string GetSecureFilePath(string fileName)
    {
        // Use LocalApplicationData as it is the standard location for non-roaming 
        // application data that does not require elevated permissions.
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appFolderPath = Path.Combine(localAppData, "ONYX Archiver");

        if (!Directory.Exists(appFolderPath))
        {
            Directory.CreateDirectory(appFolderPath);
        }

        // Extract only the filename to prevent Path Traversal attacks and 
        // ensure all keys stay within the designated local folder.
        string strippedFileName = Path.GetFileName(fileName);
        return Path.Combine(appFolderPath, strippedFileName);
    }

    /// <summary>
    /// Encrypts and saves a byte array to the secure storage.
    /// </summary>
    /// <param name="fileName">The name of the destination file.</param>
    /// <param name="key">The raw sensitive data to be protected.</param>
    public void SaveKey(string fileName, byte[] key)
    {
        string fullPath = GetSecureFilePath(fileName);

        // DPAPI: Protection at the OS user level.
        byte[] encryptedData = ProtectedData.Protect(key, Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(fullPath, encryptedData);
    }

    /// <summary>
    /// Reads and decrypts a key from the secure storage.
    /// </summary>
    /// <param name="fileName">The name of the encrypted key file.</param>
    /// <returns>The decrypted original byte array.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the requested key file is missing.</exception>
    /// <exception cref="SecureStorageException">Thrown when decryption fails (e.g., accessed by a different user profile).</exception>
    public byte[] LoadKey(string fileName)
    {
        string fullPath = GetSecureFilePath(fileName);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Secure storage file not found: {fullPath}");

        try
        {
            byte[] encryptedData = File.ReadAllBytes(fullPath);
            return ProtectedData.Unprotect(encryptedData, Entropy, DataProtectionScope.CurrentUser);
        }
        catch (CryptographicException ex)
        {
            Debug.WriteLine($"DPAPI Decryption failed: {ex.Message}");
            // Wrap in a domain-specific exception for consistent error handling in the UI/Services.
            throw new SecureStorageException("Unable to access the protected user key. The data might be corrupted or was encrypted by a different user session.", ex.Message);
        }
    }

    /// <summary>
    /// Securely deletes a key file from storage.
    /// Attempts to overwrite data before deletion to mitigate simple recovery attacks.
    /// </summary>
    /// <param name="fileName">The name of the file to remove.</param>
    public void DeleteKey(string fileName)
    {
        string fullPath = GetSecureFilePath(fileName);

        if (File.Exists(fullPath))
        {
            try
            {
                // Overwrite the file content with zeros before deletion as a basic security measure.
                byte[] dummy = new byte[new FileInfo(fullPath).Length];
                File.WriteAllBytes(fullPath, dummy);

                File.Delete(fullPath);
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"IO error during secure deletion: {ex.Message}");
                throw new SecureStorageException("Failed to securely remove the key file from disk.", ex.Message);
            }
        }
    }
}