using ZstdSharp;

namespace OnyxArchiver.Core.Services.Compression;

/// <summary>
/// Provides high-performance data compression and decompression using the Zstandard (Zstd) algorithm.
/// Zstandard offers a superior trade-off between compression ratio and speed compared to Deflate or Gzip.
/// </summary>
public sealed class ZstandardService : IDisposable
{
    private readonly Compressor _compressor;
    private readonly Decompressor _decompressor;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZstandardService"/> with a specified compression level.
    /// </summary>
    /// <param name="compressionLevel">The compression effort (typically 1-22). Default is 3, which is the standard balance.</param>
    public ZstandardService(int compressionLevel = 3)
    {
        _compressor = new Compressor(compressionLevel);
        _decompressor = new Decompressor();
    }

    /// <summary>
    /// Compresses the provided raw data.
    /// </summary>
    /// <param name="rawData">The input bytes to compress. Uses <see cref="ReadOnlySpan{T}"/> for efficient memory handling.</param>
    /// <returns>A byte array containing the Zstandard-compressed data.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the service has been disposed.</exception>
    public byte[] Compress(ReadOnlySpan<byte> rawData)
    {
        CheckDisposed();

        // Wrap performs the compression and returns a Pooled memory result
        return _compressor.Wrap(rawData).ToArray();
    }

    /// <summary>
    /// Decompresses Zstandard-compressed data back to its original form.
    /// </summary>
    /// <param name="compressedData">The bytes to decompress.</param>
    /// <param name="expectedRawSize">The original size of the data before compression (required for buffer allocation).</param>
    /// <returns>A byte array containing the original plaintext/raw bytes.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the service has been disposed.</exception>
    public byte[] Decompress(ReadOnlySpan<byte> compressedData, int expectedRawSize)
    {
        CheckDisposed();

        // Unwrap restores the data based on the provided expected size
        return _decompressor.Unwrap(compressedData, expectedRawSize).ToArray();
    }

    /// <summary>
    /// Verifies that the service is still active and has not been disposed.
    /// </summary>
    private void CheckDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ZstandardService));
    }

    /// <summary>
    /// Securely releases the native Zstandard resources used by the compressor and decompressor.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _compressor.Dispose();
        _decompressor.Dispose();
        _disposed = true;
    }
}
