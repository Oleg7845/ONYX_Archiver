namespace OnyxArchiver.Domain.Exceptions;

/// <summary>
/// Thrown when the application fails to parse, deserialize, or validate a handshake file (.onyx).
/// This is a critical barrier that prevents malformed or malicious identity data from entering the system.
/// </summary>
/// <remarks>
/// This typically occurs if:
/// 1. The file was truncated during transfer (e.g., via email or chat).
/// 2. The file format version is newer or older than what the current client supports.
/// 3. The file's internal magic bytes do not match the Onyx Handshake specification.
/// </remarks>
public class ReadHandshakeFileException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadHandshakeFileException"/> class.
    /// </summary>
    /// <param name="userMessage">Friendly message explaining why the file couldn't be read (e.g., "The file format is not recognized").</param>
    /// <param name="technicalMessage">Detailed parsing error (e.g., "Unexpected EOF at offset 128" or "Invalid version: 2.0").</param>
    public ReadHandshakeFileException(string? userMessage = null, string? technicalMessage = null)
        : base(
            userMessage: userMessage ?? "The handshake file could not be read.",
            technicalMessage: technicalMessage ?? "Handshake deserialization failed.")
    {
    }
}