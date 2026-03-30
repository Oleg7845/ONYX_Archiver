using System.Security.Cryptography;

namespace CryptoCore.Cryptography.Hashing;

/// <summary>
/// Provides utility methods for calculating SHA-512 cryptographic hashes for files.
/// This implementation focuses on memory efficiency and high-performance I/O operations.
/// </summary>
public static class FileToSha512
{
    /// <summary>
    /// Computes the SHA-512 hash of a specified file and returns it as a hexadecimal string.
    /// </summary>
    /// <param name="filePath">The absolute or relative path to the file to be hashed.</param>
    /// <returns>A uppercase hexadecimal string representing the SHA-512 hash.</returns>
    /// <exception cref="ArgumentException">Thrown when the file path is invalid.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the target file does not exist.</exception>
    /// <exception cref="IOException">Thrown when there is an error reading the file (e.g., file is locked).</exception>
    public static string HashFile(string filePath)
    {
        // Initialize the SHA-512 cryptographic provider.
        using var sha512 = SHA512.Create();

        // Open the file with Read access. 
        // We wrap it in a BufferedStream to minimize the number of direct OS read calls,
        // which significantly improves performance when processing large files.
        using var stream = new BufferedStream(
            File.OpenRead(filePath),
            1024 * 1024); // 1MB buffer size for optimal throughput.

        // Compute the hash from the stream directly to avoid loading the entire file into RAM.
        byte[] hashBytes = sha512.ComputeHash(stream);

        // Convert the raw bytes to a human-readable Hex string (e.g., 'A1B2C3...').
        return Convert.ToHexString(hashBytes);
    }
}