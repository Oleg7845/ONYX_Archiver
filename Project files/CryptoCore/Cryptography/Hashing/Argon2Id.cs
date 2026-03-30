using System.Text;
using System.Security.Cryptography;
using NSec.Cryptography;

namespace CryptoCore.Cryptography.Hashing;

/// <summary>
/// Provides high-level cryptographic operations using the Argon2id password-based key derivation function.
/// Argon2id is resistant to GPU-based brute-force attacks and side-channel timing attacks.
/// </summary>
public sealed class Argon2Id
{
    /// <summary> Default output key length in bytes. </summary>
    public const int KeyLength = 32;

    /// <summary> Default number of iterations (passes over memory). </summary>
    public const int Iterations = 3;

    /// <summary> Default memory cost in KiB (64 MiB). </summary>
    public const int MemoryKiB = 65536;

    /// <summary> Default degree of parallelism (number of threads). </summary>
    public const int Parallelism = 1;

    /// <summary> Standard salt size in bytes. </summary>
    public const int SaltSize = 16;

    /// <summary>
    /// Hashes a password using Argon2id and returns a formatted PHC string.
    /// </summary>
    /// <param name="password">The plain-text password to hash.</param>
    /// <returns>A PHC-formatted string containing parameters, salt, and the computed hash.</returns>
    public static string HashPassword(ReadOnlySpan<char> password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] pwdBytes = Encoding.UTF8.GetBytes(password.ToArray());
        try
        {
            var algorithm = CreateAlgorithm(MemoryKiB, Iterations, Parallelism);
            byte[] hash = algorithm.DeriveBytes(pwdBytes, salt, KeyLength);

            return $"$argon2id$v=19$m={MemoryKiB},t={Iterations},p={Parallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }
        finally
        {
            // Ensure sensitive password bytes are cleared from memory immediately
            CryptographicOperations.ZeroMemory(pwdBytes);
        }
    }

    /// <summary>
    /// Verifies a plain-text password against a stored PHC hash string.
    /// </summary>
    /// <param name="password">The password to verify.</param>
    /// <param name="phcHash">The stored PHC-formatted hash string.</param>
    /// <returns>True if the password matches the hash; otherwise, false.</returns>
    public static bool VerifyPassword(ReadOnlySpan<char> password, string phcHash)
    {
        var parts = phcHash.Split('$');
        if (parts.Length != 6) return false;

        try
        {
            int m = ExtractInt(parts[3], "m=");
            int t = ExtractInt(parts[3], "t=");
            int p = ExtractInt(parts[3], "p=");
            byte[] salt = Convert.FromBase64String(parts[4]);
            byte[] expectedHash = Convert.FromBase64String(parts[5]);

            byte[] pwdBytes = Encoding.UTF8.GetBytes(password.ToArray());
            try
            {
                var algorithm = CreateAlgorithm(m, t, p);
                byte[] actualHash = algorithm.DeriveBytes(pwdBytes, salt, expectedHash.Length);

                // Use fixed-time comparison to prevent timing attacks
                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(pwdBytes);
            }
        }
        catch { return false; }
    }

    /// <summary>
    /// Generates a key descriptor string containing a new random salt and Argon2id parameters.
    /// Useful for key derivation scenarios where the hash is not stored yet.
    /// </summary>
    /// <returns>A partial PHC string with version, parameters, and salt.</returns>
    public static string CreateKeyDescriptor()
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        return $"$argon2id$v=19$m={MemoryKiB},t={Iterations},p={Parallelism}${Convert.ToBase64String(salt)}";
    }

    /// <summary>
    /// Derives a cryptographic key from a password using parameters extracted from a descriptor.
    /// </summary>
    /// <param name="password">The password to derive the key from.</param>
    /// <param name="descriptor">The key descriptor string containing salt and parameters.</param>
    /// <returns>A 32-byte key for symmetric encryption.</returns>
    /// <exception cref="ArgumentException">Thrown when the descriptor format is invalid.</exception>
    /// <exception cref="CryptographicException">Thrown when memory parameters are out of safe bounds.</exception>
    public static byte[] DeriveKey(ReadOnlySpan<char> password, string descriptor)
    {
        var parts = descriptor.Split('$');
        if (parts.Length < 5) throw new ArgumentException("Invalid descriptor format");

        int m = ExtractInt(parts[3], "m=");
        int t = ExtractInt(parts[3], "t=");
        int p = ExtractInt(parts[3], "p=");
        byte[] salt = Convert.FromBase64String(parts[4]);

        // Security check for memory range
        if (m < 8192 || m > 256 * 1024) throw new CryptographicException("Invalid Argon2 memory range.");

        byte[] pwdBytes = Encoding.UTF8.GetBytes(password.ToArray());
        try
        {
            var algorithm = CreateAlgorithm(m, t, p);
            return algorithm.DeriveBytes(pwdBytes, salt, KeyLength);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pwdBytes);
        }
    }

    /// <summary>
    /// Creates an instance of the Argon2id KDF algorithm with specified parameters.
    /// </summary>
    private static PasswordBasedKeyDerivationAlgorithm CreateAlgorithm(int mKiB, int t, int p)
    {
        var argon2Params = new Argon2Parameters
        {
            MemorySize = (long)mKiB,
            NumberOfPasses = t,
            DegreeOfParallelism = p
        };
        return PasswordBasedKeyDerivationAlgorithm.Argon2id(argon2Params);
    }

    /// <summary>
    /// Extracts an integer value for a specific parameter (e.g., "m=" or "t=") from a comma-separated string.
    /// </summary>
    private static int ExtractInt(string s, string prefix)
    {
        foreach (var p in s.Split(','))
        {
            if (p.StartsWith(prefix))
                return int.Parse(p.Substring(prefix.Length));
        }
        throw new ArgumentException($"Missing parameter: {prefix}");
    }
}
