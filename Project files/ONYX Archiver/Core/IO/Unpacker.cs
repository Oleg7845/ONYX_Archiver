using CryptoCore.Abstractions;
using OnyxArchiver.Core.Models.Archive;
using OnyxArchiver.Core.Models.Archive.BlockIndex;
using OnyxArchiver.Core.Models.Archive.FileIndex;
using OnyxArchiver.Core.Models.Archive.VirtualCatalog;
using OnyxArchiver.Core.Services.Compression;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace OnyxArchiver.Core.IO;

/// <summary>
/// Service responsible for reading, verifying, decrypting, and extracting data from Onyx archives.
/// This class ensures that no data is extracted unless the archive's digital signature is valid.
/// </summary>
public class Unpacker : IDisposable
{
    private string _archivePath;

    // Metadata properties populated during the unpacking process
    public ArchiveHeader? ArchiveHeader { get; private set; }
    public VirtualFolder? VirtualFolder { get; private set; }
    public FileIndex? FileIndex { get; private set; }
    public BlockIndex? BlockIndex { get; private set; }

    private ZstandardService _compressor;
    private IDecrypter _decrypter;

    // State flags to enforce the correct execution order
    private bool _isHeaderRead = false;
    private bool _isMetadataUnpacked = false;

    // Progress tracking variables
    private long _totalSize = 0;
    private long _processedSize = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="Unpacker"/> class for a specific archive file.
    /// </summary>
    /// <param name="archivePath">Full path to the source archive file.</param>
    public Unpacker(string archivePath)
    {
        _archivePath = archivePath;
        _compressor = new ZstandardService();
    }

    /// <summary>
    /// Overloaded constructor for scenarios where metadata is already available (e.g., resumed sessions).
    /// </summary>
    public Unpacker(
        ArchiveHeader archiveHeader,
        VirtualFolder virtualFolder,
        FileIndex fileIndex,
        BlockIndex blockIndex,
        IDecrypter decrypter,
        string archivePath)
    {
        ArchiveHeader = archiveHeader;
        VirtualFolder = virtualFolder;
        FileIndex = fileIndex;
        BlockIndex = blockIndex;

        _isMetadataUnpacked = true;
        _compressor = new ZstandardService();
        _decrypter = decrypter;
        _archivePath = archivePath;
    }

    // Unpack header ====================================================================
    /// <summary>
    /// Reads the archive header located at the end of the file (before the signature).
    /// </summary>
    /// <remarks>
    /// This is the first step of unpacking, used to obtain the archive ID and encryption parameters.
    /// The method calculates the header position relative to the end of the file.
    /// </remarks>
    /// <returns>The deserialized <see cref="ArchiveHeader"/>.</returns>
    /// <exception cref="Exception">Thrown if the file magic number is invalid.</exception>
    public ArchiveHeader UnpackHeader()
    {
        using (FileStream inputStream = File.OpenRead(_archivePath))
        {
            // Verify that the file begins with the "ONYX" magic identifier
            ReadAndCheckMagic(inputStream);

            // Calculate the header offset: File Length - (Fixed Header Size + Signature Size)
            long headerOffset = inputStream.Length - (ArchiveHeader.FixedSize + ArchiveHeader.SignatureSize);
            byte[] headerBytes = new byte[ArchiveHeader.FixedSize];

            inputStream.Seek(headerOffset, SeekOrigin.Begin);
            inputStream.ReadExactly(headerBytes);

            // Convert the raw bytes back into the ArchiveHeader object
            ArchiveHeader = ArchiveHeader.Deserialize(headerBytes);
            _isHeaderRead = true;

            return ArchiveHeader;
        }
    }
    // ==================================================================================

    // Unpack metadata ==================================================================
    /// <summary>
    /// Verifies the digital signature and decrypts archive metadata (Catalog, File Index, and Block Index).
    /// </summary>
    /// <param name="decrypter">Cryptographic service for data decryption.</param>
    /// <param name="verifier">Service used to verify the archive's digital signature.</param>
    /// <param name="signaturePublicKey">Public key required for signature verification.</param>
    /// <exception cref="InvalidOperationException">Thrown if called before <see cref="UnpackHeader"/>.</exception>
    /// <exception cref="CryptographicException">Thrown if the archive signature is invalid or the file was modified.</exception>
    public void UnpackMetadata(IDecrypter decrypter, IVerifier verifier, byte[] signaturePublicKey)
    {
        if (!_isHeaderRead)
            throw new InvalidOperationException("First, the header must be unpacked using the UnpackHeader() method.");

        _decrypter = decrypter;

        using (FileStream inputStream = File.OpenRead(_archivePath))
        {
            // Integrity Check: Perform a full file hash and verify against the Ed25519 signature
            ReadSignatureAndVerifyArchive(inputStream, verifier, signaturePublicKey);

            // Decrypt the virtual folder structure (The Catalog)
            ReadCatalog(inputStream);

            // Decrypt the metadata mapping of files (The File Index)
            ReadFileIndex(inputStream);

            // Decrypt the physical layout information (The Block Index)
            ReadBlockIndex(inputStream);
        }

        _isMetadataUnpacked = true;
    }

    /// <summary>
    /// Validates the file format identifier (Magic) at the beginning of the stream.
    /// </summary>
    private void ReadAndCheckMagic(FileStream inputStream)
    {
        byte[] magicBuffer = new byte[ArchiveHeader.MagicSize];
        inputStream.ReadExactly(magicBuffer);

        string magic = Encoding.UTF8.GetString(magicBuffer, 0, ArchiveHeader.Magic.Length);

        if (magic != ArchiveHeader.Magic)
            throw new Exception("This is not a valid Onyx file or it is corrupted.");
    }

    /// <summary>
    /// Performs a full file integrity check using SHA-512 hashing and digital signature verification.
    /// This ensures that not a single byte of the archive (metadata or blocks) has been altered.
    /// </summary>
    private void ReadSignatureAndVerifyArchive(FileStream inputStream, IVerifier verifier, byte[] signaturePublicKey)
    {
        // Read the 64-byte Ed25519 signature from the very end of the file
        byte[] signatureBytes = new byte[ArchiveHeader.SignatureSize];
        inputStream.Seek(-ArchiveHeader.SignatureSize, SeekOrigin.End);
        inputStream.ReadExactly(signatureBytes);

        // Reset stream to start hashing everything except the signature itself
        inputStream.Seek(0, SeekOrigin.Begin);

        IncrementalHash hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
        long bytesToHash = inputStream.Length - ArchiveHeader.SignatureSize;
        byte[] buffer = new byte[8192];
        long readBytes = 0;

        // Stream through the file to generate the final SHA-512 hash
        while (readBytes < bytesToHash)
        {
            int toRead = (int)Math.Min(buffer.Length, bytesToHash - readBytes);
            int read = inputStream.Read(buffer, 0, toRead);
            hasher.AppendData(buffer, 0, read);
            readBytes += read;
        }

        // Validate the hash against the digital signature using the public key
        if (!verifier.VerifyRemote(
            data: hasher.GetHashAndReset(),
            signature: signatureBytes,
            publicKey: signaturePublicKey))
            throw new CryptographicException("Critical error: Signature is invalid! The archive may have been tampered with.");

        verifier.Dispose();
    }

    /// <summary>
    /// Extracts and decrypts the virtual directory structure from the archive.
    /// </summary>
    private void ReadCatalog(FileStream inputStream)
    {
        byte[] encryptedCatalogBytes = new byte[ArchiveHeader!.CatalogLength];
        inputStream.Seek(ArchiveHeader.CatalogOffset, SeekOrigin.Begin);
        inputStream.ReadExactly(encryptedCatalogBytes);

        // Decryption followed by Zstandard decompression
        byte[] rawBytes = DecryptAndDecompress(
            encryptedCatalogBytes,
            ArchiveHeader.CatalogRawLength);

        VirtualFolder = VirtualFolder.Deserialize(rawBytes);
    }

    /// <summary>
    /// Extracts and decrypts the file index containing file metadata and segments mapping.
    /// </summary>
    private void ReadFileIndex(FileStream inputStream)
    {
        byte[] encryptedFileIndexBytes = new byte[ArchiveHeader!.FileIndexLength];
        inputStream.Seek(ArchiveHeader.FileIndexOffset, SeekOrigin.Begin);
        inputStream.ReadExactly(encryptedFileIndexBytes);

        byte[] rawBytes = DecryptAndDecompress(
            encryptedFileIndexBytes,
            ArchiveHeader.FileIndexRawLength);

        FileIndex = FileIndex.Deserialize(rawBytes);
    }

    /// <summary>
    /// Extracts and decrypts the block index containing offsets and lengths of encrypted data blocks.
    /// </summary>
    private void ReadBlockIndex(FileStream inputStream)
    {
        byte[] encryptedBlockIndexBytes = new byte[ArchiveHeader!.BlockIndexLength];
        inputStream.Seek(ArchiveHeader.BlockIndexOffset, SeekOrigin.Begin);
        inputStream.ReadExactly(encryptedBlockIndexBytes);

        byte[] rawBytes = DecryptAndDecompress(
            encryptedBlockIndexBytes,
            ArchiveHeader.BlockIndexRawLength);

        BlockIndex = BlockIndex.Deserialize(rawBytes);
    }
    // ==================================================================================

    // Unpack files =====================================================================
    /// <summary>
    /// Extracts all files contained in the archive to the specified destination, restoring the directory tree.
    /// </summary>
    /// <param name="destinationPath">The root folder where files will be extracted.</param>
    /// <param name="cancellationToken">Token to cancel the extraction process.</param>
    /// <param name="progress">Progress reporter for UI updates.</param>
    public void UnpackAllFiles(
        string destinationPath,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        if (!_isMetadataUnpacked)
            throw new InvalidOperationException("First, the metadata must be unpacked using the UnpackMetadata() method.");

        var files = VirtualFolder!.GetAllFiles();
        _totalSize = files.Sum(f => f.Size);
        _processedSize = 0;

        // Recreate the folder structure on disk before writing files
        RestoreFolderStructure(VirtualFolder!.Subfolders.FirstOrDefault()!, destinationPath);

        using FileStream fs = File.OpenRead(_archivePath);

        foreach (var virtualFile in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Resolve target file path using platform-specific directory separators
            string targetPath = Path.Combine(destinationPath, virtualFile.Path.Replace('/', Path.DirectorySeparatorChar));
            ExtractFileInternal(fs, virtualFile, targetPath, cancellationToken, progress);
        }
    }

    /// <summary>
    /// Extracts a single file identified by its GUID to the destination path.
    /// Allows for efficient "Selective Extraction" without processing the whole archive.
    /// </summary>
    /// <param name="fileId">Unique identifier of the file in the virtual catalog.</param>
    /// <param name="destinationPath">The folder where the file will be saved.</param>
    public void UnpackFile(
        Guid fileId,
        string destinationPath,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        if (!_isMetadataUnpacked)
            throw new InvalidOperationException("First, the metadata must be unpacked using the UnpackMetadata() method.");

        VirtualFile virtualFile = VirtualFolder!.GetFileById(fileId);

        if (virtualFile == null)
            throw new FileNotFoundException($"File with ID {fileId} not found in archive.");

        _totalSize = virtualFile.Size;
        _processedSize = 0;

        using FileStream fs = File.OpenRead(_archivePath);

        string targetPath = Path.Combine(destinationPath, Path.GetFileName(virtualFile.Path));
        ExtractFileInternal(fs, virtualFile, targetPath, cancellationToken, progress);
    }

    /// <summary>
    /// Core logic for file assembly: reads individual encrypted blocks, decrypts them, and streams to a file.
    /// </summary>
    private void ExtractFileInternal(
        FileStream inputStream,
        VirtualFile virtualFile,
        string outputPath,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        // Get the list of segments (blocks) that make up this specific file
        FileEntity fileEntity = FileIndex!.GetEntityById(virtualFile.Id)!;
        FileStream? outputStream = null;

        try
        {
            outputStream = File.Create(outputPath);

            foreach (var segment in fileEntity.Segments)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Look up physical block location in the Block Index
                BlockEntity block = BlockIndex!.GetEntityById(segment.BlockId)!;

                byte[] encryptedBytes = new byte[block.Length];
                inputStream.Seek(block.Offset, SeekOrigin.Begin);
                inputStream.ReadExactly(encryptedBytes);

                // Reconstruct the plaintext block
                byte[] rawData = DecryptAndDecompress(encryptedBytes, block.RawLength);

                outputStream.Write(rawData);

                // Update processed size and report progress percentage
                _processedSize += rawData.Length;

                if (progress != null)
                {
                    double percentage = _totalSize == 0 ? 100 : ((double)_processedSize / _totalSize) * 100;
                    percentage = Math.Min(percentage, 100.0);

                    progress.Report(new ProgressReport
                    {
                        Percentage = percentage,
                        CurrentFile = virtualFile.Name
                    });
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            // If the operation is canceled, cleanup the partially written file
            if (File.Exists(outputPath))
            {
                outputStream?.Dispose();
                File.Delete(outputPath);
                Debug.WriteLine($"[Unpacker] Extraction canceled: {outputPath}");
            }
            return;
        }
        finally
        {
            outputStream?.Dispose();
        }
    }

    /// <summary>
    /// Recursively creates the directory structure on the physical disk based on the <see cref="VirtualFolder"/> model.
    /// </summary>
    private void RestoreFolderStructure(VirtualFolder folder, string currentPath)
    {
        string fullPath = Path.Combine(currentPath, folder.Name);

        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        if (folder.Subfolders != null)
        {
            foreach (var subFolder in folder.Subfolders)
            {
                RestoreFolderStructure(subFolder, fullPath);
            }
        }
    }
    // ==================================================================================

    /// <summary>
    /// Helper method to perform combined decryption and Zstandard decompression of a data packet.
    /// Uses the ArchiveId as Associated Authenticated Data (AAD) for validation.
    /// </summary>
    private byte[] DecryptAndDecompress(byte[] encryptedData, long expectedRawLength)
    {
        // Decrypt using the session-specific ID to ensure the block belongs to this archive
        byte[] decrypted = _decrypter!.Decrypt(encryptedData, ArchiveHeader!.ArchiveId.ToByteArray());

        // Decompress the validated plaintext
        return _compressor.Decompress(decrypted, (int)expectedRawLength);
    }

    /// <summary>
    /// Releases all cryptographic and system resources.
    /// </summary>
    public void Dispose()
    {
        _compressor.Dispose();
        _decrypter.Dispose();
    }
}