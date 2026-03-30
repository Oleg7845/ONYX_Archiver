using CryptoCore.Abstractions;
using OnyxArchiver.Core.Models.Archive;
using OnyxArchiver.Core.Models.Archive.BlockIndex;
using OnyxArchiver.Core.Models.Archive.FileIndex;
using OnyxArchiver.Core.Models.Archive.VirtualCatalog;
using OnyxArchiver.Core.Services.Compression;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace OnyxArchiver.Core.IO;

/// <summary>
/// Core engine responsible for packing files and directories into a secure, 
/// compressed, and digitally signed Onyx archive format.
/// </summary>
public class Packer : IDisposable
{
    // Internal state and indexing
    private Guid _archiveId;
    private string _sourcePath;
    private string _destinationPath;
    private string? _archiveName;
    private VirtualFolder? _virtualFolder;
    private FileIndex _fileIndex;
    private BlockIndex _blocksIndex;

    // Cryptographic and compression services
    private ZstandardService _compressor;
    private IEncrypter _encrypter;
    private ISigner _signer;
    private byte[] _salt;
    private byte[] _publicKey;
    private byte[] _keyIdentifier;
    private IncrementalHash _hasher;

    /// <summary>
    /// Defines the size of each data block before compression (4 MB).
    /// </summary>
    private const int BlockSize = 4 * 1024 * 1024;

    // Metadata for archive structure: Offsets and lengths of the internal sections
    private int _lastBlockIndex = 0;
    private long _catalogOffset = 0;
    private long _catalogLength = 0;
    private long _catalogRawLength = 0;
    private long _fileIndexOffset = 0;
    private long _fileIndexLength = 0;
    private long _fileIndexRawLength = 0;
    private long _blockIndexOffset = 0;
    private long _blockIndexLength = 0;
    private long _blockIndexRawLength = 0;

    // Progress tracking data
    private long _totalSize = 0;
    private long _processedSize = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="Packer"/> class with required cryptographic primitives.
    /// </summary>
    /// <param name="archiveId">Unique identifier for the archive session.</param>
    /// <param name="sourcePath">The path to the file or folder to be packed.</param>
    /// <param name="destinationPath">The directory where the resulting archive will be saved.</param>
    /// <param name="encrypter">Service used for authenticated encryption of data blocks.</param>
    /// <param name="signer">Service used for creating a digital signature of the entire archive.</param>
    /// <param name="peerId">Identifier of the target recipient.</param>
    /// <param name="salt">Cryptographic salt used for key derivation.</param>
    /// <param name="publicKey">The ephemeral public key included in the archive header.</param>
    /// <param name="keyIdentifier">Identifier for the key used to facilitate decryption.</param>
    public Packer(
        Guid archiveId,
        string sourcePath,
        string destinationPath,
        IEncrypter encrypter,
        ISigner signer,
        Guid peerId,
        byte[] salt,
        byte[] publicKey,
        byte[] keyIdentifier)
    {
        _archiveId = archiveId;
        _sourcePath = sourcePath;
        _destinationPath = destinationPath;
        _fileIndex = new FileIndex();
        _blocksIndex = new BlockIndex();
        _compressor = new ZstandardService();
        _encrypter = encrypter;
        _signer = signer;
        _salt = salt;
        _publicKey = publicKey;
        _keyIdentifier = keyIdentifier;
        _hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
    }

    /// <summary>
    /// Starts the packing process: scans the source, writes data blocks, and appends the metadata header.
    /// </summary>
    /// <param name="archiveName">The name of the archive file to create.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <param name="progress">An optional progress reporter to track the percentage of completion.</param>
    /// <exception cref="OperationCanceledException">Thrown when the task is canceled via <paramref name="cancellationToken"/>.</exception>
    public void Pack(
        string archiveName,
        CancellationToken cancellationToken = default,
        IProgress<ProgressReport>? progress = null)
    {
        _archiveName = archiveName;

        // Initialize virtual file system structure by scanning the source path
        if (Directory.Exists(_sourcePath))
        {
            _virtualFolder = new VirtualFolder(archiveName);
            _virtualFolder.Subfolders.Add(FolderScanner.ScanFolder(_sourcePath));
        }
        else if (File.Exists(_sourcePath))
            _virtualFolder = FolderScanner.ScanFile(_sourcePath);

        // Prepare progress reporting data by calculating total source size
        var files = _virtualFolder!.GetAllFiles();
        _totalSize = files.Sum(f => f.Size);
        _processedSize = 0;

        // Define the output file path using the specific Onyx extension
        string archivePath = System.IO.Path.Combine(
            _destinationPath,
            $"{archiveName}.{ArchiveHeader.Magic.ToLower()}");

        FileStream? outputStream = null;

        try
        {
            outputStream = File.Create(archivePath);

            // Write the Magic Identifier to the beginning of the file for format validation
            byte[] magicBytes = new byte[ArchiveHeader.MagicSize];
            Encoding.UTF8.GetBytes(ArchiveHeader.Magic).CopyTo(magicBytes, 0);

            outputStream.Write(magicBytes);
            _hasher.AppendData(magicBytes); // Add magic bytes to the incremental hash

            // Initialize the read buffer for block-based processing
            byte[] buffer = new byte[BlockSize];
            int bufferOffset = 0;

            // Iterate through every file in the virtual folder and pack it
            foreach (VirtualFile file in _virtualFolder.GetAllFiles())
            {
                cancellationToken.ThrowIfCancellationRequested();

                using FileStream inputFileStream = File.OpenRead(file.Path);

                // Create a FileEntity record for the current file in the global index
                var fileEntity = new FileEntity(file.Id);
                _fileIndex.Add(fileEntity);

                int readBytes;

                // Read file data into the buffer and process as full blocks
                while ((readBytes = inputFileStream.Read(buffer, bufferOffset, BlockSize - bufferOffset)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    bufferOffset += readBytes;

                    // When the buffer is full, process it as a single block
                    if (bufferOffset == BlockSize)
                    {
                        ProcessAndWriteBlock(outputStream, buffer, bufferOffset, fileEntity);
                        bufferOffset = 0;
                    }

                    // Update and report progress
                    _processedSize += readBytes;
                    if (progress != null)
                    {
                        progress.Report(new ProgressReport
                        {
                            Percentage = (double)_processedSize / _totalSize * 100,
                            CurrentFile = file.Name
                        });
                    }
                }

                // Handle remaining bytes that didn't fill a full block
                if (bufferOffset > 0)
                {
                    ProcessAndWriteBlock(outputStream, buffer, bufferOffset, fileEntity);
                    bufferOffset = 0;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Finalize the archive by appending metadata, headers, and the digital signature
            FinalizePacking(outputStream);
        }
        catch (OperationCanceledException)
        {
            // Cleanup on cancellation: close stream and delete partial file
            if (outputStream != null)
            {
                outputStream.Dispose();
                outputStream = null;
            }

            if (File.Exists(archivePath))
                File.Delete(archivePath);

            throw;
        }
        finally
        {
            outputStream?.Dispose();
        }
    }

    /// <summary>
    /// Compresses, encrypts, and writes a single block of data to the output stream.
    /// </summary>
    /// <param name="outputStream">The stream to write the processed block to.</param>
    /// <param name="data">The raw data buffer.</param>
    /// <param name="length">The number of bytes to process from the buffer.</param>
    /// <param name="fileEntity">The file metadata entry associated with this block.</param>
    private void ProcessAndWriteBlock(FileStream outputStream, byte[] data, int length, FileEntity fileEntity)
    {
        // Extract the exact amount of data to be processed from the larger buffer
        byte[] cleanData = new byte[length];
        Array.Copy(data, cleanData, length);

        // Perform compression and encryption sequence
        byte[] encryptedPacket = CompressAndEncrypt(cleanData);

        // Include the encrypted packet in the rolling hash for final signing
        _hasher.AppendData(encryptedPacket);

        // Record the current file position before writing the block
        long currentPosition = outputStream.Position;
        outputStream.Write(encryptedPacket);

        // Assign a unique block identifier
        _lastBlockIndex++;

        // Add the block entry to the BlockIndex for random access and integrity checks
        _blocksIndex.Add(
            new BlockEntity(
                blockId: _lastBlockIndex,
                offset: currentPosition,
                length: encryptedPacket.Length,
                rawLength: length));

        // Link this specific block segment to the current file entity
        fileEntity.Add(
            new FileSegment(
                blockId: _lastBlockIndex,
                offset: currentPosition,
                length: length));
    }

    /// <summary>
    /// Finalizes the archive by writing the catalog, file/block indexes, the main header, and the digital signature.
    /// </summary>
    /// <param name="outputStream">The archive file stream.</param>
    private void FinalizePacking(FileStream outputStream)
    {
        // 1. Write the directory structure (Catalog)
        WriteCatalog(outputStream);

        // 2. Write the file metadata (FileIndex)
        WriteFileIndex(outputStream);

        // 3. Write the physical block layout (BlockIndex)
        WriteBlockIndex(outputStream);

        // 4. Construct and write the Master Header containing pointers to all metadata
        CreateAndWriteArchiveHeader(outputStream);

        // 5. Sign the entire archive hash and append the signature for authenticity verification
        SignArchiveAndWriteSignature(outputStream);
    }

    /// <summary>
    /// Serializes and writes the virtual folder structure (Catalog) to the archive.
    /// </summary>
    private void WriteCatalog(FileStream outputStream)
    {
        byte[] rootFolderBytes = _virtualFolder!.Serialize();

        // Catalog data is also compressed and encrypted
        byte[] rootFolderEncryptedBytes = CompressAndEncrypt(rootFolderBytes);

        _hasher.AppendData(rootFolderEncryptedBytes);

        // Record the physical location of the Catalog in the file
        _catalogOffset = outputStream.Position;
        _catalogLength = rootFolderEncryptedBytes.Length;
        _catalogRawLength = rootFolderBytes.Length;

        outputStream.Write(rootFolderEncryptedBytes);
    }

    /// <summary>
    /// Serializes, compresses, and encrypts the file index, then writes it to the output stream.
    /// </summary>
    private void WriteFileIndex(FileStream outputStream)
    {
        byte[] fileIndexBytes = _fileIndex.Serialize();

        byte[] fileIndexEncryptedBytes = CompressAndEncrypt(fileIndexBytes);

        _hasher.AppendData(fileIndexEncryptedBytes);

        // Record the physical location of the File Index
        _fileIndexOffset = outputStream.Position;
        _fileIndexLength = fileIndexEncryptedBytes.Length;
        _fileIndexRawLength = fileIndexBytes.Length;

        outputStream.Write(fileIndexEncryptedBytes);
    }

    /// <summary>
    /// Serializes and writes the block-to-file mapping index to the archive.
    /// </summary>
    private void WriteBlockIndex(FileStream outputStream)
    {
        byte[] blocksIndexBytes = _blocksIndex.Serialize();

        byte[] blocksIndexEncryptedBytes = CompressAndEncrypt(blocksIndexBytes);

        _hasher.AppendData(blocksIndexEncryptedBytes);

        // Record the physical location of the Block Index
        _blockIndexOffset = outputStream.Position;
        _blockIndexLength = blocksIndexEncryptedBytes.Length;
        _blockIndexRawLength = blocksIndexBytes.Length;

        outputStream.Write(blocksIndexEncryptedBytes);
    }

    /// <summary>
    /// Constructs the final archive header containing all metadata offsets and writes it to the stream.
    /// </summary>
    private void CreateAndWriteArchiveHeader(FileStream outputStream)
    {
        // Build the header object with all previously calculated offsets
        ArchiveHeader header = new ArchiveHeader(
            archiveId: _archiveId,
            salt: _salt,
            publicKey: _publicKey,
            keyIdentifier: _keyIdentifier,
            catalogOffset: _catalogOffset,
            catalogLength: _catalogLength,
            catalogRawLength: _catalogRawLength,
            fileIndexOffset: _fileIndexOffset,
            fileIndexLength: _fileIndexLength,
            fileIndexRawLength: _fileIndexRawLength,
            blockIndexOffset: _blockIndexOffset,
            blockIndexLength: _blockIndexLength,
            blockIndexRawLength: _blockIndexRawLength);

        byte[] headerBytes = header.Serialize();
        outputStream.Write(headerBytes);

        // The header is included in the hash before signing
        _hasher.AppendData(headerBytes);
    }

    /// <summary>
    /// Signs the accumulated archive hash and writes the final signature to the stream.
    /// </summary>
    private void SignArchiveAndWriteSignature(FileStream outputStream)
    {
        // Get the final SHA-512 hash and sign it using Ed25519
        outputStream.Write(
            _signer.Sign(
                _hasher.GetHashAndReset()));
    }

    /// <summary>
    /// Applies compression then encryption to a data array.
    /// </summary>
    /// <param name="data">Raw byte array to process.</param>
    /// <returns>Compressed and encrypted byte array.</returns>
    private byte[] CompressAndEncrypt(byte[] data)
    {
        // Ensure data is compressed before encryption to maximize efficiency and security
        return _encrypter.Encrypt(
            data: _compressor.Compress(data),
            associatedData: _archiveId.ToByteArray()); // Use archive ID as Associated Authenticated Data (AAD)
    }

    /// <summary>
    /// Releases all cryptographic and system resources used by the packer.
    /// </summary>
    public void Dispose()
    {
        _compressor.Dispose();
        _encrypter.Dispose();
        _signer.Dispose();
        _hasher.Dispose();
    }
}