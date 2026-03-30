using CryptoCore.Abstractions;
using CryptoCore.Cryptography.Keys.Ed25519;
using CryptoCore.Cryptography.Keys.X25519;
using OnyxArchiver.Core.Models.KeyExcahnge;

namespace OnyxArchiver.Core.KeyExchange;

/// <summary>
/// Orchestrates the cryptographic handshake process between two parties.
/// Handles the generation, exchange, and secure storage of ephemeral X25519 and Ed25519 keys.
/// </summary>
/// <remarks>
/// This manager ensures that public keys are signed for authenticity and 
/// private keys are always encrypted before being returned to the caller.
/// </remarks>
public class KeyExchangeManager
{
    /// <summary>
    /// Initiates a new handshake by generating a fresh key pair and a signed <see cref="KeyExchangeContext"/>.
    /// </summary>
    /// <param name="encrypter">The encryption provider used to protect the generated private keys.</param>
    /// <returns>A <see cref="KeyExchangeBundle"/> containing the public context and encrypted private keys.</returns>
    public static KeyExchangeBundle CreateHandshake(IEncrypter encrypter)
    {
        using var X25519KCxt = new X25519KeyContext();
        using var Ed25519KCxt = new Ed25519KeyContext();

        using (var ed25519Provider = new Ed25519Provider(Ed25519KCxt))
        {
            // Create a 'Begin' context containing public keys and a digital signature
            var kexContext = new KeyExchangeContext(
                type: KeyExchangeContextType.Begin,
                encryptionPublicKey: X25519KCxt.PublicKey,
                signaturePublicKey: Ed25519KCxt.PublicKey,
                signer: ed25519Provider);

            return new KeyExchangeBundle(
                context: kexContext,
                privateEncryptionKey: X25519KCxt.ExportEncrypted(encrypter),
                privateSignatureKey: Ed25519KCxt.ExportEncrypted(encrypter));
        }
    }

    /// <summary>
    /// Responds to an incoming handshake request from another party.
    /// Verifies the sender's identity and generates a reciprocal key set.
    /// </summary>
    /// <param name="handshakeBytes">The serialized <see cref="KeyExchangeContext"/> received from the initiator.</param>
    /// <param name="encrypter">The encryption provider for protecting the local private keys.</param>
    /// <returns>A bundle containing the response context and protected local keys.</returns>
    public static KeyExchangeBundle AcceptHandshake(byte[] handshakeBytes, IEncrypter encrypter)
    {
        using var X25519KCxt = new X25519KeyContext();
        using var Ed25519KCxt = new Ed25519KeyContext();

        using (var ed25519Provider = new Ed25519Provider(Ed25519KCxt))
        {
            // Validate the initiator's signature and extract their public keys
            var handshakeBegin = KeyExchangeContext.Deserialize(
                data: handshakeBytes,
                verifier: ed25519Provider);

            // Create an 'End' context as a response
            var kexContext = new KeyExchangeContext(
                id: handshakeBegin.Id,
                type: KeyExchangeContextType.End,
                encryptionPublicKey: X25519KCxt.PublicKey,
                signaturePublicKey: Ed25519KCxt.PublicKey,
                signer: ed25519Provider);

            return new KeyExchangeBundle(
                context: kexContext,
                privateEncryptionKey: X25519KCxt.ExportEncrypted(encrypter),
                privateSignatureKey: Ed25519KCxt.ExportEncrypted(encrypter),
                publicEncryptionKey: encrypter.Encrypt(handshakeBegin.EncryptionPublicKey),
                publicSignatureKey: encrypter.Encrypt(handshakeBegin.SignaturePublicKey));
        }
    }

    /// <summary>
    /// Finalizes the handshake on the initiator's side by processing the respondent's public keys.
    /// </summary>
    /// <param name="handshakeBytes">The serialized response from the other party.</param>
    /// <returns>A verified <see cref="KeyExchangeContext"/> containing the remote party's public keys.</returns>
    public static KeyExchangeContext FinalizeHandshake(byte[] handshakeBytes)
    {
        using var Ed25519KCxt = new Ed25519KeyContext();

        using (var ed25519Provider = new Ed25519Provider(Ed25519KCxt))
        {
            // Verify the respondent's signature before completing the exchange
            return KeyExchangeContext.Deserialize(
                data: handshakeBytes,
                verifier: ed25519Provider);
        }
    }
}
