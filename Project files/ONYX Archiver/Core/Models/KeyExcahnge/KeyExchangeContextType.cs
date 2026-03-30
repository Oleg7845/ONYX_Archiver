namespace OnyxArchiver.Core.Models.KeyExcahnge;

/// <summary>
/// Specifies the state or role of a <see cref="KeyExchangeContext"/> within a cryptographic handshake.
/// </summary>
public enum KeyExchangeContextType : ushort
{
    /// <summary>
    /// Represents the initial handshake request (Initiator). 
    /// Contains the sender's ephemeral public keys and identity proof.
    /// </summary>
    Begin = 0,

    /// <summary>
    /// Represents the final handshake response (Respondent). 
    /// Contains the receiver's public keys and confirmation of the exchange.
    /// </summary>
    End = 1
}
