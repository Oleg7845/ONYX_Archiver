namespace OnyxArchiver.UI.Helpers;

/// <summary>
/// Provides static validation logic for enforcing master password complexity requirements.
/// This ensures that derived cryptographic keys are resistant to brute-force attacks.
/// </summary>
public static class PasswordValidator
{
    /// <summary>
    /// Validates a password string against defined security rules.
    /// </summary>
    /// <param name="password">The raw password string to validate.</param>
    /// <returns>
    /// A string containing a localized error message if validation fails; 
    /// otherwise, <c>null</c> if the password meets all criteria.
    /// </returns>
    public static string? GetValidationError(string password)
    {
        if (string.IsNullOrEmpty(password))
            return "Password cannot be empty.";

        if (password.Length < 8)
            return "Password is too short (minimum 8 characters).";

        if (!password.Any(char.IsUpper))
            return "Add at least one uppercase letter.";

        if (!password.Any(char.IsLower))
            return "Add at least one lowercase letter.";

        if (!password.Any(char.IsDigit))
            return "Password must contain at least one digit.";

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            return "Add at least one special character (e.g., !@#$%^&*).";

        return null;
    }
}
