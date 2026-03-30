using CryptoCore.Cryptography.Encryption.XChaCha20;
using CryptoCore.Cryptography.Hashing;
using CryptoCore.Cryptography.Keys.Ed25519;
using CryptoCore.Cryptography.Keys.X25519;
using OnyxArchiver.Domain.Entities;
using OnyxArchiver.Domain.Exceptions;
using OnyxArchiver.Domain.Interfaces;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace OnyxArchiver.Infrastructure.Services;

/// <summary>
/// Manages user authentication and account lifecycle.
/// Implements secure password hashing via Argon2id and multi-stage key derivation.
/// </summary>
/// <remarks>
/// This service acts as the gateway to the user's "vault", transforming a human-readable 
/// password into cryptographic-grade symmetric keys for key-wrapping.
/// </remarks>
public class AuthService : IAuthService
{
    private IUserRepository _userRepository;
    private ISecureStorageService _secureStorageService;

    /// <summary> Gets whether a user is currently authenticated in the application. </summary>
    public bool IsLoggedIn { get; private set; }

    /// <summary> Gets the username of the currently logged-in user. </summary>
    public string Username { get; private set; } = string.Empty;

    public AuthService(IUserRepository userRepository, ISecureStorageService secureStorageService)
    {
        _userRepository = userRepository;
        _secureStorageService = secureStorageService;
    }

    /// <summary>
    /// Authenticates a user, derives their master symmetric key, and stores it 
    /// securely in the OS-level storage.
    /// </summary>
    /// <param name="username">The identifier of the user.</param>
    /// <param name="password">The plaintext password to verify.</param>
    /// <exception cref="UserNotFoundException">Thrown if the user does not exist.</exception>
    /// <exception cref="InvalidPasswordException">Thrown if hashing verification fails.</exception>
    public async Task Login(string username, string password)
    {
        byte[]? argon2IdKey = null;
        byte[]? symmetricKey = null;

        try
        {
            UserEntity? userEntity = await _userRepository.GetByUsernameAsync(username);

            if (userEntity == null)
                throw new UserNotFoundException(username);

            if (!Argon2Id.VerifyPassword(password, userEntity.PasswordHash))
                throw new InvalidPasswordException();

            argon2IdKey = Argon2Id.DeriveKey(password, userEntity.KeyDescriptor);

            symmetricKey = Hkdf.DeriveKey(
                inputKeyingMaterial: argon2IdKey,
                salt: userEntity.Salt,
                info: Encoding.UTF8.GetBytes(userEntity.Username))
                .Key;

            _secureStorageService.SaveKey(
                fileName: username,
                key: symmetricKey);

            IsLoggedIn = true;
            Username = username;
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            Debug.WriteLine($"Critical error: {ex.Message}");
            throw new DomainException("Login error", ex.Message);
        }
        finally
        {
            if (argon2IdKey != null)
                CryptographicOperations.ZeroMemory(argon2IdKey);

            if (symmetricKey != null)
                CryptographicOperations.ZeroMemory(symmetricKey);
        }
    }

    /// <summary>
    /// Handles new user registration. Generates unique salts, hashes the password, 
    /// and creates encrypted X25519/Ed25519 identity keys.
    /// </summary>
    /// <param name="username">The desired unique username.</param>
    /// <param name="password">The master password used to protect account keys.</param>
    /// <exception cref="UserAlreadyExistsException">Thrown if the name is already taken.</exception>
    public async Task Registration(string username, string password)
    {
        byte[]? argon2IdKey = null;
        byte[]? symmetricKey = null;

        try
        {
            if (await _userRepository.ExistsAsync(username))
                throw new UserAlreadyExistsException(username);

            string keyDescriptor = Argon2Id.CreateKeyDescriptor();

            byte[] salt = Hkdf.GenerateRandomSalt();
            byte[] usernameBytes = Encoding.UTF8.GetBytes(username);

            argon2IdKey = Argon2Id.DeriveKey(password, keyDescriptor);

            symmetricKey = Hkdf.DeriveKey(
                inputKeyingMaterial: argon2IdKey,
                salt: salt,
                info: usernameBytes)
                .Key;

            using var encrypter = new XChaCha20Poly1305Provider(
                key: symmetricKey,
                associatedData: usernameBytes);

            using var X25519KCxt = new X25519KeyContext();
            using var Ed25519KCxt = new Ed25519KeyContext();

            var userEntity = new UserEntity
            {
                Username = username,
                PasswordHash = Argon2Id.HashPassword(password),
                KeyDescriptor = keyDescriptor,
                Salt = salt,
                EncryptionPrivateKey = X25519KCxt.ExportEncrypted(encrypter),
                SignaturePrivateKey = Ed25519KCxt.ExportEncrypted(encrypter)
            };

            await _userRepository.AddAsync(userEntity);
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            Debug.WriteLine($"Critical error: {ex.Message}");
            throw new DomainException("Account creation error", ex.Message);
        }
        finally
        {
            if (argon2IdKey != null)
                CryptographicOperations.ZeroMemory(argon2IdKey);

            if (symmetricKey != null)
                CryptographicOperations.ZeroMemory(symmetricKey);
        }
    }

    /// <summary>
    /// Terminates the current session by wiping sensitive keys from secure storage 
    /// and clearing memory identifiers.
    /// </summary>
    public async Task Logout()
    {
        try
        {
            _secureStorageService.DeleteKey(fileName: Username);
            IsLoggedIn = false;
            Username = string.Empty;
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            Debug.WriteLine($"Critical error: {ex}");
            throw new DomainException("Logout error", ex.Message);
        }
    }
}
