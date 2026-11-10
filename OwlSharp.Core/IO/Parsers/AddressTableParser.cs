using System.Collections.Frozen;
using BymlLibrary;
using OwlSharp.Core.IO.Buffers;

namespace OwlSharp.Core.IO.Parsers;

public static class AddressTableParser
{
    public static FrozenDictionary<string, string> ParseAddressTable(in ReadOnlySpan<byte> src, OtZstd zstd)
    {
        using var decompressed = RentedBuffer<byte>.Allocate(OtZstd.GetDecompressedSize(src));
        zstd.Decompress(src, decompressed.Span);

        return Byml.FromBinary(decompressed.Span)
            .GetMap()
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetString())
            .ToFrozenDictionary();
    }
}