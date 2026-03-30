using CryptoCore.Abstractions;
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
/// Provides administrative actions for the authenticated user, such as credential rotation 
/// and account decommissioning.
/// </summary>
public class UserService : IUserService
{
    private readonly IAuthService _authService;
    private readonly ISecureStorageService _secureStorageService;
    private readonly IUserRepository _userRepository;
    private readonly IPeerRepository _peerRepository;

    public UserService(
        IAuthService authService,
        ISecureStorageService secureStorageService,
        IUserRepository userRepository,
        IPeerRepository peerRepository)
    {
        _authService = authService;
        _secureStorageService = secureStorageService;
        _userRepository = userRepository;
        _peerRepository = peerRepository;
    }

    /// <summary>
    /// Performs a secure password rotation. 
    /// Re-encrypts the user's permanent identity keys (X25519 and Ed25519) using a new 
    /// symmetric key derived from the new password.
    /// </summary>
    /// <param name="currentPassword">The existing password to authorize the change.</param>
    /// <param name="newPassword">The new password to protect the vault.</param>
    /// <exception cref="UserNotFoundException">Thrown if the session user is missing from the DB.</exception>
    /// <exception cref="InvalidPasswordException">Thrown if current password verification fails.</exception>
    public async Task UpdateUserPasswordAsync(string currentPassword, string newPassword)
    {
        // Buffers for sensitive key material to be wiped later
        byte[]? currentSymmetricKey = null;
        byte[]? argon2IdKey = null;
        byte[]? newSymmetricKey = null;

        try
        {
            // Retrieve user entity by username associated with the current session
            UserEntity? userEntity = await _userRepository.GetByUsernameAsync(_authService.Username);

            if (userEntity == null)
                throw new UserNotFoundException(_authService.Username);

            // Validate current password before allowing any changes
            if (!Argon2Id.VerifyPassword(currentPassword, userEntity.PasswordHash))
                throw new InvalidPasswordException();

            byte[] usernameBytes = Encoding.UTF8.GetBytes(_authService.Username);

            // Fetch the existing master symmetric key from secure storage
            currentSymmetricKey = _secureStorageService.LoadKey(_authService.Username);

            // Initialize decrypter to unlock current identity keys
            using IDecrypter decrypter = new XChaCha20Poly1305Provider(
                key: currentSymmetricKey!,
                associatedData: usernameBytes);

            // Decrypt and restore the long-term asymmetric keys into memory
            using var restoredX25519KCxt = X25519KeyContext.ImportEncrypted(userEntity.EncryptionPrivateKey, decrypter);
            using var restoredEd25519KCxt = Ed25519KeyContext.ImportEncrypted(userEntity.SignaturePrivateKey, decrypter);

            // Start derivation process for the new credentials
            string keyDescriptor = Argon2Id.CreateKeyDescriptor();
            byte[] salt = Hkdf.GenerateRandomSalt();

            // Derive a new intermediate key from the new password using Argon2Id
            argon2IdKey = Argon2Id.DeriveKey(newPassword, keyDescriptor);

            // Derive the final master symmetric key using HKDF
            newSymmetricKey = Hkdf.DeriveKey(
                inputKeyingMaterial: argon2IdKey,
                salt: salt,
                info: usernameBytes).Key;

            // Initialize encrypter with the new master key
            using IEncrypter encrypter = new XChaCha20Poly1305Provider(
                key: newSymmetricKey,
                associatedData: usernameBytes);

            // Update entity with new hashes and re-encrypted private keys
            userEntity.PasswordHash = Argon2Id.HashPassword(newPassword);
            userEntity.KeyDescriptor = keyDescriptor;
            userEntity.Salt = salt;
            userEntity.EncryptionPrivateKey = restoredX25519KCxt.ExportEncrypted(encrypter);
            userEntity.SignaturePrivateKey = restoredEd25519KCxt.ExportEncrypted(encrypter);

            // Update the physical key file in secure storage
            _secureStorageService.SaveKey(
                fileName: _authService.Username,
                key: newSymmetricKey);

            // Commit changes to the database
            if (!await _userRepository.UpdateAsync(userEntity))
                throw new UserUpdatingException(userEntity.Username);
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            Debug.WriteLine($"Critical error during password rotation: {ex.Message}");
            throw new DomainException("Password Rotation Failure", ex.Message);
        }
        finally
        {
            // Securely wipe sensitive buffers from RAM to prevent heap inspection attacks
            if (currentSymmetricKey != null) CryptographicOperations.ZeroMemory(currentSymmetricKey);
            if (argon2IdKey != null) CryptographicOperations.ZeroMemory(argon2IdKey);
            if (newSymmetricKey != null) CryptographicOperations.ZeroMemory(newSymmetricKey);
        }
    }

    /// <summary>
    /// Permanently removes the user account and purges all associated peer relationships.
    /// </summary>
    /// <param name="password">Password confirmation for security verification.</param>
    public async Task DeleteUserAsync(string password)
    {
        try
        {
            UserEntity? userEntity = await _userRepository.GetByUsernameAsync(_authService.Username);

            if (userEntity == null)
                throw new UserNotFoundException(_authService.Username);

            // Identity verification before destructive action
            if (!Argon2Id.VerifyPassword(password, userEntity.PasswordHash))
                throw new InvalidPasswordException();

            // Cleanup: Retrieve and delete all associated peer entities
            List<PeerEntity>? peers = await _peerRepository.GetAllPeersAsync(userEntity.Username);

            if (peers != null)
            {
                foreach (PeerEntity peer in peers)
                {
                    await _peerRepository.DeleteAsync(peer.Id);
                }
            }

            // Final account deletion
            if (!await _userRepository.DeleteAsync(userEntity.Id))
                throw new UserDeletingException(userEntity.Username);
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            Debug.WriteLine($"Critical error during user deletion: {ex.Message}");
            throw new DomainException("Account Deletion Failure", ex.Message);
        }
    }
}