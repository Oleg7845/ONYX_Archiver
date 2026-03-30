using CryptoCore.Abstractions;
using CryptoCore.Cryptography.Encryption.XChaCha20;
using CryptoCore.Cryptography.Hashing;
using CryptoCore.Cryptography.Keys.X25519;
using OnyxArchiver.Core.Models.Cryptography;
using OnyxArchiver.Domain.Entities;
using OnyxArchiver.Domain.Exceptions;
using OnyxArchiver.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace OnyxArchiver.Infrastructure.Services.Security;

/// <summary>
/// Orchestrates complex cryptographic workflows for user identity and archive security.
/// Manages the retrieval of private keys and the derivation of ephemeral session keys 
/// using X25519 Diffie-Hellman and HKDF.
/// </summary>
/// <remarks>
/// This service ensures that raw keying material is never left in memory longer than necessary 
/// by utilizing <see cref="CryptographicOperations.ZeroMemory"/>.
/// </remarks>
public class UserVaultService : IUserVaultService
{
    public ISecureStorageService _secureStorageService;
    public IAuthService _authService;
    public IUserRepository _userRepository;

    public UserVaultService(ISecureStorageService secureStorageService, IAuthService authService, IUserRepository userRepository)
    {
        _secureStorageService = secureStorageService;
        _authService = authService;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Generates a complete <see cref="EncryptionContext"/> for a new archive.
    /// Performs an ephemeral-static Diffie-Hellman exchange to create a unique symmetric key.
    /// </summary>
    /// <returns>A context containing a fresh salt, ephemeral public key, and a configured encrypter.</returns>
    /// <exception cref="EncryptionContextException">Thrown if the user key cannot be loaded or derivation fails.</exception>
    public async Task<EncryptionContext> GetEncryptionContext()
    {
        byte[]? sharedSecret = null;
        byte[]? symmetricKey = null;

        try
        {
            byte[] usernameBytes = Encoding.UTF8.GetBytes(_authService.Username);

            using X25519KeyContext userX25519KCxt = await GetUserKeyContext(usernameBytes);

            using var ephemeralX25519KCxt = new X25519KeyContext();

            byte[] salt = Hkdf.GenerateRandomSalt();

            sharedSecret = ephemeralX25519KCxt.DeriveSharedSecret(userX25519KCxt.PublicKey);

            symmetricKey = Hkdf.DeriveKey(
                inputKeyingMaterial: sharedSecret,
                salt: salt,
                info: usernameBytes).Key;

            return new EncryptionContext(
                salt: salt,
                publicKey: ephemeralX25519KCxt.PublicKey,
                encrypter: new XChaCha20Poly1305Provider(
                    key: symmetricKey,
                    associatedData: usernameBytes));
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            throw new EncryptionContextException("Failed to prepare encryption context", ex.Message);
        }
        finally
        {
            if (sharedSecret != null)
                CryptographicOperations.ZeroMemory(sharedSecret);

            if (symmetricKey != null)
                CryptographicOperations.ZeroMemory(symmetricKey);
        }
    }

    /// <summary>
    /// Prepares an <see cref="IEncrypter"/> instance to process data for an existing archive metadata.
    /// </summary>
    public async Task<IEncrypter> GetEncrypter(byte[] salt, byte[] publicKey)
    {
        try
        {
            return await GetXChaCha20Poly1305Provider(salt, publicKey);
        }
        catch (DomainException ex)
        {
            throw new EncryptionProviderException("Failed to prepare encrypter", ex.Message);
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            throw new EncryptionProviderException("Failed to prepare encrypter", ex.Message);
        }
    }

    /// <summary>
    /// Prepares an <see cref="IDecrypter"/> instance by re-deriving the shared secret 
    /// from the stored user private key and the archive's ephemeral public key.
    /// </summary>
    public async Task<IDecrypter> GetDecrypter(byte[] salt, byte[] publicKey)
    {
        try
        {
            return await GetXChaCha20Poly1305Provider(salt, publicKey);
        }
        catch (DomainException ex)
        {
            throw new EncryptionProviderException("Failed to prepare decrypter", ex.Message);
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            throw new EncryptionProviderException("Failed to prepare decrypter", ex.Message);
        }
    }

    /// <summary>
    /// Internal helper to derive a symmetric session key using X25519 and HKDF.
    /// </summary>
    /// <param name="salt">The salt stored in the archive header.</param>
    /// <param name="publicKey">The ephemeral public key stored in the archive header.</param>
    /// <returns>A configured provider for XChaCha20-Poly1305 authenticated encryption.</returns>
    private async Task<XChaCha20Poly1305Provider> GetXChaCha20Poly1305Provider(byte[] salt, byte[] publicKey)
    {
        byte[]? sharedSecret = null;
        byte[]? symmetricKey = null;

        try
        {
            byte[] usernameBytes = Encoding.UTF8.GetBytes(_authService.Username);

            using X25519KeyContext userX25519KCxt = await GetUserKeyContext(usernameBytes);

            sharedSecret = userX25519KCxt.DeriveSharedSecret(publicKey);

            symmetricKey = Hkdf.DeriveKey(
                inputKeyingMaterial: sharedSecret,
                salt: salt,
                info: usernameBytes).Key;

            return new XChaCha20Poly1305Provider(
                key: symmetricKey,
                associatedData: usernameBytes);
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            throw new EncryptionProviderException("Failed to prepare encryption provider", ex.Message);
        }
        finally
        {
            if (sharedSecret != null)
                CryptographicOperations.ZeroMemory(sharedSecret);

            if (symmetricKey != null)
                CryptographicOperations.ZeroMemory(symmetricKey);
        }
    }

    /// <summary>
    /// Loads the user's permanent X25519 private key.
    /// The key is decrypted on-the-fly using the master key retrieved from <see cref="ISecureStorageService"/>.
    /// </summary>
    /// <param name="usernameBytes">User identifier used as associated data for decryption.</param>
    /// <returns>A <see cref="X25519KeyContext"/> containing the decrypted private key.</returns>
    private async Task<X25519KeyContext> GetUserKeyContext(byte[] usernameBytes)
    {
        byte[]? symmetricKey = null;

        try
        {
            UserEntity? userEntity = await _userRepository.GetByUsernameAsync(_authService.Username);

            if (userEntity == null)
                throw new UserNotFoundException(_authService.Username);

            symmetricKey = _secureStorageService.LoadKey(_authService.Username);

            using IDecrypter decrypter = new XChaCha20Poly1305Provider(
                key: symmetricKey,
                associatedData: usernameBytes);

            return X25519KeyContext.ImportEncrypted(userEntity.EncryptionPrivateKey, decrypter);
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            throw new EncryptionContextException("Failed to prepare encryption context", ex.Message);
        }
        finally
        {
            if (symmetricKey != null)
                CryptographicOperations.ZeroMemory(symmetricKey);
        }
    }
}
