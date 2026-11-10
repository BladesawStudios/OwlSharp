using BymlLibrary;
using OwlSharp.Core.IO.Buffers;

namespace OwlSharp.Core.IO.Parsers;

public static class EventFlowFileEntryParser
{
    public static Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> ParseFileEntry(Span<byte> src, OtZstd zstd)
    {
        using var decompressed = RentedBuffer<byte>.Allocate(OtZstd.GetDecompressedSize(src));
        zstd.Decompress(src, decompressed.Span);
        
        return Byml.FromBinary(decompressed.Span)
            .GetMap()["Versions"]
            .GetMap()
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetString())
            .GetAlternateLookup<ReadOnlySpan<char>>();
    }
}