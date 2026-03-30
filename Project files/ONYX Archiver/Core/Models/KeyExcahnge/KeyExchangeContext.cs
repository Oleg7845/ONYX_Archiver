using CryptoCore.Abstractions;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace OnyxArchiver.Core.Models.KeyExcahnge;

/// <summary>
/// Represents a signed container for public keys exchanged during a handshake.
/// This model handles binary serialization and ensures data integrity through digital signatures.
/// </summary>
/// <remarks>
/// The context includes a Magic header and Version to identify the protocol, 
/// along with a unique ID to prevent replay or mismatching handshake steps.
/// </remarks>
public class KeyExchangeContext : IDisposable
{
    private ISigner? _signer;
    private IVerifier? _verifier;

    /// <summary> Protocol magic identifier: "ONXK". </summary>
    public const string Magic = "ONXK";

    /// <summary> Current protocol version. </summary>
    public const ushort Version = 1;

    /// <summary> Unique identifier for this specific handshake session. </summary>
    public Guid Id { get; set; }

    /// <summary> The type of context (0 for Begin, 1 for End). </summary>
    public ushort Type { get; set; }  // Begin: 0, End: 1

    /// <summary> The raw 32-byte X25519 public key. </summary>
    public byte[] EncryptionPublicKey { get; set; }

    /// <summary> The raw 32-byte Ed25519 public key. </summary>
    public byte[] SignaturePublicKey { get; set; }

    /// <summary> The 64-byte Ed25519 signature of the metadata and public keys. </summary>
    public byte[]? Signature { get; set; }

    // Size constants for fixed-length binary layout
    public const int MagicSize = 4;
    public const int VersionSize = 2;
    public const int IdSize = 16;
    public const int TypeSize = 2;
    public const int EncryptionPublicKeySize = 32;
    public const int SignaturePublicKeySize = 32;
    public const int SignatureSize = 64;

    /// <summary> Total size of the serialized context in bytes (152 bytes). </summary>
    public const int FixedSize =
        MagicSize +
        VersionSize +
        IdSize +
        TypeSize +
        EncryptionPublicKeySize +
        SignaturePublicKeySize +
        SignatureSize;

    /// <summary>
    /// Internal constructor that handles validation, signing, and verification logic.
    /// </summary>
    private KeyExchangeContext(
        string magic,
        ushort version,
        Guid id,
        ushort type,
        byte[] encryptionPublicKey,
        byte[] signaturePublicKey,
        IVerifier? verifier = null,
        ISigner? signer = null,
        byte[]? signature = null)
    {
        if (string.IsNullOrWhiteSpace(magic) || Encoding.UTF8.GetByteCount(magic) != Magic.Length)
            throw new ArgumentException($"Magic must be exactly {MagicSize} UTF8 bytes.");

        if (magic != Magic)
            throw new ArgumentException($"Magic must be \"{Magic}\"");

        if (version != Version)
            throw new ArgumentException($"Version must be \"{Version}\"");

        if (!Enum.IsDefined((KeyExchangeContextType)type))
            throw new ArgumentException($"The value {type} is not within the valid range for KeyExchangeContextType.");

        if (encryptionPublicKey.Length != EncryptionPublicKeySize)
            throw new ArgumentException($"Encryption public key must be {EncryptionPublicKeySize} bytes.");

        if (signaturePublicKey.Length != SignaturePublicKeySize)
            throw new ArgumentException($"Signature public key must be {SignaturePublicKeySize} bytes.");

        if (signature != null && signature.Length != SignatureSize)
            throw new ArgumentException($"Signature must be {SignatureSize} bytes.");

        Id = id;
        Type = type;
        EncryptionPublicKey = encryptionPublicKey;
        SignaturePublicKey = signaturePublicKey;
        Signature = signature;

        _signer = signer;
        _verifier = verifier;

        if (_signer != null && _verifier == null && Signature == null)
        {
            Signature = _signer.Sign(data: GetSiganturePayload());
        }

        if (_signer == null && _verifier != null && Signature != null)
        {
            if (!_verifier.VerifyRemote(
                data: GetSiganturePayload(),
                signature: Signature,
                publicKey: SignaturePublicKey))
                throw new CryptographicException("KeyExchangeContext signature verification failed. The data might be corrupted or tampered with.");
        }
    }

    // Creation constructor (begin)
    public KeyExchangeContext(
        KeyExchangeContextType type,
        byte[] encryptionPublicKey,
        byte[] signaturePublicKey,
        ISigner signer) : this(
            Magic,
            Version,
            Guid.NewGuid(),
            (ushort)type,
            encryptionPublicKey,
            signaturePublicKey,
            signer: signer)
    { }

    // Creation constructor (end)
    public KeyExchangeContext(
        Guid id,
        KeyExchangeContextType type,
        byte[] encryptionPublicKey,
        byte[] signaturePublicKey,
        ISigner signer) : this(
            Magic,
            Version,
            id,
            (ushort)type,
            encryptionPublicKey,
            signaturePublicKey,
            signer: signer)
    { }

    // Deserialize constructor
    public KeyExchangeContext(
        Guid id,
        ushort type,
        byte[] encryptionPublicKey,
        byte[] signaturePublicKey,
        byte[] signature,
        IVerifier verifier) : this(
            Magic,
            Version,
            id,
            (ushort)type,
            encryptionPublicKey,
            signaturePublicKey,
            signature: signature,
            verifier: verifier)
    { }

    /// <summary>
    /// Serializes the context into a fixed-size byte array for transmission.
    /// </summary>
    /// <returns>A 152-byte array containing the signed handshake packet.</returns>
    public byte[] Serialize()
    {
        using var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms, Encoding.UTF8))
        {
            bw.Write(Encoding.UTF8.GetBytes(Magic));
            bw.Write(Version);
            bw.Write(Id.ToByteArray());
            bw.Write(Type);
            bw.Write(EncryptionPublicKey);
            bw.Write(SignaturePublicKey);
            bw.Write(Signature!);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Deserializes a byte array into a <see cref="KeyExchangeContext"/> and validates the signature.
    /// </summary>
    /// <param name="data">The 152-byte packet.</param>
    /// <param name="verifier">The verifier used to validate the packet's authenticity.</param>
    /// <returns>A validated context instance.</returns>
    /// <exception cref="CryptographicException">Thrown if the signature is invalid or data is tampered with.</exception>
    public static KeyExchangeContext Deserialize(byte[] data, IVerifier verifier)
    {
        if (data.Length != FixedSize)
            throw new ArgumentException($"Deserialized data must be exactly {FixedSize} bytes in size.");

        using var ms = new MemoryStream(data);
        using (var br = new BinaryReader(ms, Encoding.UTF8))
        {
            return new KeyExchangeContext(
                magic: Encoding.UTF8.GetString(br.ReadBytes(MagicSize)),
                version: br.ReadUInt16(),
                id: new Guid(br.ReadBytes(IdSize)),
                type: ReadAndCheckType(br.ReadUInt16()),
                encryptionPublicKey: br.ReadBytes(EncryptionPublicKeySize),
                signaturePublicKey: br.ReadBytes(SignaturePublicKeySize),
                signature: br.ReadBytes(SignatureSize),
                verifier: verifier);
        }
    }

    /// <summary>
    /// Validates that the raw <see cref="ushort"/> value read from the binary stream 
    /// corresponds to a valid member of the <see cref="KeyExchangeContextType"/> enumeration.
    /// </summary>
    private static ushort ReadAndCheckType(ushort type)
    {
        if (!Enum.IsDefined((KeyExchangeContextType)type))
            throw new ArgumentException($"The value {type} is not within the valid range for KeyExchangeContextType.");

        return type;
    }

    /// <summary>
    /// Prepares the byte payload that is subject to digital signing.
    /// Includes all header information and public keys.
    /// </summary>
    private byte[] GetSiganturePayload()
    {
        using var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms, Encoding.UTF8))
        {
            bw.Write(Encoding.UTF8.GetBytes(Magic));
            bw.Write(Version);
            bw.Write(Id.ToByteArray());
            bw.Write(Type);
            bw.Write(EncryptionPublicKey);
            bw.Write(SignaturePublicKey);
        }
        return ms.ToArray();
    }

    /// <summary>
    /// Wipes public keys from memory and clears sensitive references.
    /// </summary>
    public void Dispose()
    {
        if (EncryptionPublicKey != null)
            Array.Clear(EncryptionPublicKey, 0, EncryptionPublicKey.Length);

        if (SignaturePublicKey != null)
            Array.Clear(SignaturePublicKey, 0, SignaturePublicKey.Length);

        if (Signature != null)
            Array.Clear(Signature, 0, Signature.Length);

        _signer = null;
        _verifier = null;
    }
}
