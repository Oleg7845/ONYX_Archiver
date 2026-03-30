namespace OnyxArchiver.Domain.Exceptions;

/// <summary>
/// Represents a base class for exceptions occurring within the domain layer.
/// This class enforces a strict separation between human-readable messages for the UI 
/// and technical details intended for logs and debugging.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Gets a friendly, localized message intended for display to the end-user.
    /// Example: "The password you entered is incorrect."
    /// </summary>
    public string UserMessage { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="userMessage">The sanitized message shown to the user in the UI (e.g., "Archive extraction failed").</param>
    /// <param name="technicalMessage">Detailed error information for developers (e.g., "Zstd decompression error at offset 0x04").</param>
    public DomainException(string? userMessage, string? technicalMessage = null)
        : base(technicalMessage ?? userMessage ?? "Application exception")
    {
        // Fallback to a generic error if no message is provided to ensure the UI isn't empty.
        UserMessage = userMessage ?? "An unexpected error occurred. Please try again.";
    }
}