using NSec.Cryptography;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.IO.Pem;
using System.Security.Cryptography;
using System.Text;

namespace CryptoCore.Formatting.Pem;

/// <summary>
/// Provides high-level utility methods for serializing and deserializing Ed25519 private keys 
/// using the PEM format, supporting both plain PKCS#8 and encrypted PKES2 (PBKDF2/AES-256-CBC).
/// </summary>
public sealed class PemEd25519IO
{
    /// <summary>
    /// Exports and saves an Ed25519 private key to a file in PEM format.
    /// </summary>
    /// <param name="key">The <see cref="Key"/> instance to export.</param>
    /// <param name="filePath">Target file path.</param>
    /// <param name="password">Optional password for PBES2 encryption. If empty, the key is saved as plain PKCS#8.</param>
    /// <param name="iterationCount">Number of PBKDF2 iterations. Default is 600,000 for high brute-force resistance.</param>
    public static void SaveEd25519PrivateKeyPem(
        Key key,
        string filePath,
        ReadOnlySpan<char> password = default,
        int iterationCount = 600_000)
    {
        ArgumentNullException.ThrowIfNull(key);

        // Export the key in the standard PKIX (PKCS#8) format.
        byte[] pkcs8 = key.Export(KeyBlobFormat.PkixPrivateKey);

        try
        {
            byte[] output;

            if (password.IsEmpty)
            {
                output = pkcs8;
            }
            else
            {
                // Encrypt the PKCS#8 payload using AES-256-CBC with PBKDF2 derivation.
                output = EncryptPkcs8(pkcs8, password, iterationCount);
            }

            using var sw = new StringWriter();
            var writer = new Org.BouncyCastle.OpenSsl.PemWriter(sw);

            string type = password.IsEmpty
                ? "PRIVATE KEY"
                : "ENCRYPTED PRIVATE KEY";

            writer.WriteObject(new PemObject(type, output));
            writer.Writer.Flush();

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(filePath, sw.ToString());
        }
        finally
        {
            // Securely wipe the sensitive plaintext key material from memory.
            CryptographicOperations.ZeroMemory(pkcs8);
        }
    }

    /// <summary>
    /// Loads and reconstructs an Ed25519 private key from a PEM file.
    /// </summary>
    /// <param name="filePath">Path to the PEM file.</param>
    /// <param name="password">The password required to decrypt an ENCRYPTED PRIVATE KEY.</param>
    /// <returns>A <see cref="Key"/> instance initialized with the imported material.</returns>
    /// <exception cref="CryptographicException">Thrown if the file is malformed, the password is incorrect, or the iteration count is unsafe.</exception>
    public static Key LoadEd25519PrivateKeyPem(
        string filePath,
        ReadOnlySpan<char> password = default)
    {
        var text = File.ReadAllText(filePath);

        const string beginEncrypted = "-----BEGIN ENCRYPTED PRIVATE KEY-----";
        const string beginPrivate = "-----BEGIN PRIVATE KEY-----";

        byte[] pkcs8;

        if (text.Contains(beginEncrypted))
        {
            if (password.IsEmpty)
                throw new CryptographicException("Password is required for an encrypted PEM file.");

            pkcs8 = ExtractAndDecodePem(text);
            pkcs8 = DecryptPkcs8(pkcs8, password);
        }
        else if (text.Contains(beginPrivate))
        {
            pkcs8 = ExtractAndDecodePem(text);
        }
        else
        {
            throw new CryptographicException("The file format is not a recognized Ed25519 PEM structure.");
        }

        try
        {
            // Re-instantiate the NSec Key object from the raw PKCS#8 bytes.
            return Key.Import(
                SignatureAlgorithm.Ed25519,
                pkcs8,
                KeyBlobFormat.PkixPrivateKey);
        }
        finally
        {
            // Ensure any decrypted or raw key bytes are wiped immediately after import.
            CryptographicOperations.ZeroMemory(pkcs8);
        }
    }

    /// <summary>
    /// Manually extracts the Base64 content from between PEM headers and footers.
    /// </summary>
    private static byte[] ExtractAndDecodePem(string pem)
    {
        var lines = pem
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        var base64 = new StringBuilder();

        foreach (var line in lines)
        {
            // Skip the header/footer lines (e.g., -----BEGIN...-----)
            if (line.StartsWith("-----"))
                continue;

            base64.Append(line.Trim());
        }

        return Convert.FromBase64String(base64.ToString());
    }

    /// <summary>
    /// Encapsulates the PKCS#8 data into an EncryptedPrivateKeyInfo (PBES2) structure.
    /// </summary>
    private static byte[] EncryptPkcs8(
        byte[] pkcs8,
        ReadOnlySpan<char> password,
        int iterations)
    {
        // Allocate buffer for UTF8 password bytes to allow for secure ZeroMemory wiping.
        byte[] pwdBytes = new byte[Encoding.UTF8.GetByteCount(password)];

        try
        {
            var random = new SecureRandom();

            // Generate a 256-bit salt for PBKDF2 and a 128-bit IV for AES-CBC.
            byte[] salt = new byte[32];
            random.NextBytes(salt);

            byte[] iv = new byte[16];
            random.NextBytes(iv);

            Encoding.UTF8.GetBytes(password, pwdBytes);

            // Setup PBKDF2-HMAC-SHA256 derivation.
            var kdf = new Pkcs5S2ParametersGenerator(new Sha256Digest());
            kdf.Init(pwdBytes, salt, iterations);

            var key = (KeyParameter)kdf.GenerateDerivedParameters("AES", 256);

            // Execute AES-256-CBC encryption with PKCS7 padding.
            var cipher = CipherUtilities.GetCipher("AES/CBC/PKCS7");
            cipher.Init(true, new ParametersWithIV(key, iv));

            byte[] encrypted = cipher.DoFinal(pkcs8);

            // Build ASN.1 structures according to RFC 8018 (PKCS #5: Password-Based Cryptography Specification).
            var pbkdf2Params = new Pbkdf2Params(salt, iterations);
            var kdfAlg = new AlgorithmIdentifier(PkcsObjectIdentifiers.IdPbkdf2, pbkdf2Params);
            var encScheme = new AlgorithmIdentifier(NistObjectIdentifiers.IdAes256Cbc, new DerOctetString(iv));

            var pbes2 = new DerSequence(kdfAlg, encScheme);
            var algId = new AlgorithmIdentifier(PkcsObjectIdentifiers.IdPbeS2, pbes2);

            var encInfo = new EncryptedPrivateKeyInfo(algId, encrypted);
            return encInfo.GetEncoded();
        }
        finally
        {
            // Clear temporary buffers containing the password or plaintext key.
            CryptographicOperations.ZeroMemory(pwdBytes);
            CryptographicOperations.ZeroMemory(pkcs8);
        }
    }

    /// <summary>
    /// Decrypts a PBES2-encoded byte array to recover the raw PKCS#8 key material.
    /// </summary>
    private static byte[] DecryptPkcs8(
        byte[] encryptedData,
        ReadOnlySpan<char> password)
    {
        byte[] pwdBytes = new byte[Encoding.UTF8.GetByteCount(password)];

        try
        {
            // Parse the ASN.1 structure.
            var encInfo = EncryptedPrivateKeyInfo.GetInstance(encryptedData);

            if (!PkcsObjectIdentifiers.IdPbeS2.Equals(encInfo.EncryptionAlgorithm.Algorithm))
                throw new CryptographicException("The provided data is not encrypted with a supported PBES2 algorithm.");

            var seq = (Asn1Sequence)encInfo.EncryptionAlgorithm.Parameters;
            if (seq.Count != 2)
                throw new CryptographicException("Malformed PBES2 structure.");

            var kdfAlg = AlgorithmIdentifier.GetInstance(seq[0]);
            var encScheme = AlgorithmIdentifier.GetInstance(seq[1]);

            // Validate that we are using PBKDF2 and AES-256-CBC.
            if (!kdfAlg.Algorithm.Equals(PkcsObjectIdentifiers.IdPbkdf2))
                throw new CryptographicException("Unsupported KDF algorithm.");

            if (!encScheme.Algorithm.Equals(NistObjectIdentifiers.IdAes256Cbc))
                throw new CryptographicException("Unsupported encryption scheme.");

            var pbkdf2 = Pbkdf2Params.GetInstance(kdfAlg.Parameters);

            // Security Policy: Reject keys with dangerously low iteration counts (brute-force protection).
            if (pbkdf2.IterationCount.IntValue < 100_000)
                throw new CryptographicException("The key uses an unsafe PBKDF2 iteration count.");

            byte[] salt = pbkdf2.GetSalt();
            if (salt.Length < 16)
                throw new CryptographicException("The key derivation salt is too short to be secure.");

            int iterations = pbkdf2.IterationCount.IntValue;
            byte[] iv = ((DerOctetString)encScheme.Parameters).GetOctets();

            Encoding.UTF8.GetBytes(password, pwdBytes);

            // Re-derive the key using the parameters extracted from the ASN.1 file.
            var kdf = new Pkcs5S2ParametersGenerator(new Sha256Digest());
            kdf.Init(pwdBytes, salt, iterations);

            var key = (KeyParameter)kdf.GenerateDerivedParameters("AES", 256);

            var cipher = CipherUtilities.GetCipher("AES/CBC/PKCS7");
            cipher.Init(false, new ParametersWithIV(key, iv));

            return cipher.DoFinal(encInfo.GetEncryptedData());
        }
        catch (Exception ex) when (ex is not CryptographicException)
        {
            throw new CryptographicException("Authentication failed: invalid password or corrupted file structure.", ex);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pwdBytes);
        }
    }
}