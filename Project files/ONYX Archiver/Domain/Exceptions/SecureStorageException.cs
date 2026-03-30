namespace OnyxArchiver.Domain.Exceptions;

/// <summary>
/// Thrown when the application fails to interact with OS-level secure storage (e.g., Windows DPAPI, macOS Keychain).
/// This represents a critical failure in the root-of-trust, preventing the app from retrieving master keys.
/// </summary>
/// <remarks>
/// These errors are typically environment-specific:
/// 1. The user's Windows profile is corrupted, making DPAPI inaccessible.
/// 2. The application lacks the necessary entitlements/permissions to access the Keychain.
/// 3. The underlying OS security service is not running or has crashed.
/// </remarks>
public class SecureStorageException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecureStorageException"/> class.
    /// </summary>
    /// <param name="userMessage">Friendly message like "Access to the system's secure vault was denied."</param>
    /// <param name="technicalMessage">Low-level OS error details (e.g., HRESULT codes or Win32 error messages).</param>
    public SecureStorageException(string? userMessage = null, string? technicalMessage = null)
        : base(
            userMessage: userMessage ?? "The system's secure storage could not be accessed.",
            technicalMessage: technicalMessage ?? "OS-level security service failure.")
    {
    }
}