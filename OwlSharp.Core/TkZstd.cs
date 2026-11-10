using Revrs.Extensions;
using OwlSharp.Core.IO.Buffers;
using ZstdSharp;
using ZsDicSharp;

namespace OwlSharp.Core;

public class TkZstd : IDisposable
{
    public const uint ZSTD_MAGIC = 0xFD2FB528;
    private readonly ZsDic _zsDic;
    
    public TkZstd(in Stream zsDicPack) : this(ReadAll(zsDicPack)) { }

    public TkZstd(in Span<byte> zsDicPack)
    {
        _zsDic = zsDicPack.IsEmpty ? ZsDic.Dictionaryless() : ZsDic.FromPack(zsDicPack);
    }

    public int CompressionLevel { get; set; } = 7;

    public byte[] Decompress(ReadOnlySpan<byte> data) => Decompress(data, out _);

    public void Decompress(ReadOnlySpan<byte> data, Span<byte> dst) => Decompress(data, dst, out _);

    public RentedBuffer<byte> Decompress(in Stream stream) => Decompress(stream, out _);
    
    public RentedBuffer<byte> Decompress(in Stream stream, out int zsDictionaryId)
    {
        var src = RentedBuffer<byte>.Allocate(stream);
        var srcSpan = src.Span;
        if (srcSpan.Length < 4 || srcSpan.Read<uint>() != ZSTD_MAGIC) {
            zsDictionaryId = -1;
            return src;
        }

        try {
            var dst = RentedBuffer<byte>.Allocate(
                GetDecompressedSize(src.Span));
            Decompress(src.Span, dst.Span, out zsDictionaryId);
            return dst;
        }
        finally {
            src.Dispose();
        }
    }

    public byte[] Decompress(ReadOnlySpan<byte> data, out int zsDictionaryId)
    {
        var size = GetDecompressedSize(data);
        var result = new byte[size];
        Decompress(data, result, out zsDictionaryId);
        return result;
    }

    public void Decompress(ReadOnlySpan<byte> data, Span<byte> dst, out int zsDictionaryId)
    {
        if (!IsCompressed(data)) {
            zsDictionaryId = -1;
            return;
        }

        zsDictionaryId = GetDictionaryId(data);
        _zsDic.Decompress(data, dst);
    }

    public RentedBuffer<byte> Compress(ReadOnlySpan<byte> data, int zsDictionaryId = -1)
    {
        var bounds = Compressor.GetCompressBound(data.Length);
        var result = RentedBuffer<byte>.Allocate(bounds);
        var size = Compress(data, result.Span, zsDictionaryId);
        result.Resize(size);
        return result;
    }

    public int Compress(ReadOnlySpan<byte> data, Span<byte> dst, int zsDictionaryId = -1) =>
        _zsDic.Compress(data, dst, zsDictionaryId < 0 ? 0u : (uint)zsDictionaryId, CompressionLevel);

    public static bool IsCompressed(ReadOnlySpan<byte> data)
    {
        return data.Length > 3 &&
               data.Read<uint>() == ZSTD_MAGIC;
    }

    public static bool IsCompressed(in Stream stream)
    {
        if (stream.Length < sizeof(uint)) {
            return false;
        }
        
        var result = stream.Read<uint>() == ZSTD_MAGIC;
        stream.Seek(-sizeof(uint), SeekOrigin.Current);
        return result;
    }

    public static int GetDecompressedSize(in Stream stream)
    {
        Span<byte> header = stackalloc byte[14];
        _ = stream.Read(header);
        stream.Seek(-14, SeekOrigin.Current);
        return GetFrameContentSize(header);
    }

    public static int GetDecompressedSize(ReadOnlySpan<byte> data)
    {
        return GetFrameContentSize(data);
    }

    public static int GetDictionaryId(ReadOnlySpan<byte> buffer)
    {
        var descriptor = buffer[4];
        var windowDescriptorSize = ((descriptor & 0b00100000) >> 5) ^ 0b1;
        var dictionaryIdFlag = descriptor & 0b00000011;

        return dictionaryIdFlag switch {
            0x0 => -1,
            0x1 => buffer[5 + windowDescriptorSize],
            0x2 => buffer[(5 + windowDescriptorSize)..].Read<ushort>(),
            0x3 => buffer[(5 + windowDescriptorSize)..].Read<int>(),
            _ => throw new OverflowException(
                "Two bits cannot exceed 0x3, something terrible has happened!")
        };
    }

    private static int GetFrameContentSize(ReadOnlySpan<byte> buffer)
    {
        var descriptor = buffer[4];
        var windowDescriptorSize = ((descriptor & 0b00100000) >> 5) ^ 0b1;
        var dictionaryIdFlag = descriptor & 0b00000011;
        var frameContentFlag = descriptor >> 6;

        var offset = dictionaryIdFlag switch {
            0x0 => 5 + windowDescriptorSize,
            0x1 => 5 + windowDescriptorSize + 1,
            0x2 => 5 + windowDescriptorSize + 2,
            0x3 => 5 + windowDescriptorSize + 4,
            _ => throw new OverflowException(
                "Two bits cannot exceed 0x3, something terrible has happened!")
        };

        return frameContentFlag switch {
            0x0 => buffer[offset],
            0x1 => buffer[offset..].Read<ushort>() + 0x100,
            0x2 => buffer[offset..].Read<int>(),
            _ => throw new NotSupportedException(
                "64-bit file sizes are not supported.")
        };
    }

    private static byte[] ReadAll(Stream stream)
    {
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _zsDic.Dispose();
    }
}