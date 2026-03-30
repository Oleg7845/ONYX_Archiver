using CryptoCore.Cryptography.Keys.Ed25519;
using CryptoCore.Cryptography.Hashing;
using System.Text;

namespace CryptoCore.Cryptography.Signing;

/// <summary>
/// Provides high-level services for file integrity and authenticity.
/// Orchestrates the process of key management, file hashing (SHA-512), 
/// and Ed25519 digital signature generation/verification.
/// </summary>
public static class FileSignatureService
{
    /// <summary>
    /// Generates a new Ed25519 key pair and exports the private key to a PEM file.
    /// </summary>
    /// <param name="filePath">Target path for the private key PEM file.</param>
    /// <param name="password">Optional password for private key encryption (PBES2).</param>
    /// <returns>The Base64-encoded public key associated with the generated private key.</returns>
    public static string CreatePemPrivateKey(string filePath, string? password = null)
    {
        // Ed25519KeyContext manages the lifecycle of the underlying cryptographic key.
        using (var ed25519KeyContext = new Ed25519KeyContext())
        {
            // Export the key using Span-based password handling for memory safety.
            ed25519KeyContext.ExportPrivateKeyToPem(filePath, password.AsSpan());

            // Return the public key so it can be distributed for signature verification.
            return Convert.ToBase64String(ed25519KeyContext.PublicKey);
        }
    }

    /// <summary>
    /// Signs a file by computing its SHA-512 hash and signing that hash with an Ed25519 private key.
    /// </summary>
    /// <param name="filePath">The file to be signed.</param>
    /// <param name="pemFilePath">Path to the Ed25519 private key PEM file.</param>
    /// <param name="password">Password for the encrypted private key (if applicable).</param>
    /// <returns>A Base64-encoded digital signature.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">Thrown on invalid password or malformed key.</exception>
    public static string SignFile(string filePath, string pemFilePath, string? password = null)
    {
        // Import the existing private key context from the encrypted storage.
        using (var ed25519KeyContext = Ed25519KeyContext.ImportPrivateKeyFromPem(pemFilePath, password.AsSpan()))
        {
            // First, calculate a unique fingerprint of the file.
            string fileHash = FileToSha512.HashFile(filePath);

            // Sign the UTF-8 representation of the hash string.
            // Note: Signing the hash (fixed length) is more efficient than signing raw file bytes.
            byte[] signature = ed25519KeyContext.Sign(
                Encoding.UTF8.GetBytes(fileHash));

            return Convert.ToBase64String(signature);
        }
    }

    /// <summary>
    /// Verifies the authenticity of a file against a provided signature and public key.
    /// </summary>
    /// <param name="filePath">The file to verify.</param>
    /// <param name="signature">The Base64-encoded signature to check.</param>
    /// <param name="publicKey">The Base64-encoded public key of the signer.</param>
    /// <returns>True if the file is authentic and has not been tampered with; otherwise, false.</returns>
    public static bool VerifyFile(string filePath, string signature, string publicKey)
    {
        // Calculate the hash of the current file on disk.
        string fileHash = FileToSha512.HashFile(filePath);

        // Perform the Ed25519 verification against the calculated hash.
        return Ed25519KeyContext.VerifyRemote(
            data: Encoding.UTF8.GetBytes(fileHash),
            signature: Convert.FromBase64String(signature),
            remotePublicKey: Convert.FromBase64String(publicKey));
    }
}