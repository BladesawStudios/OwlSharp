using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using OwlSharp.Core.IO.Buffers;

namespace OwlSharp.Core.IO.Parsers;

public static class EffectInfoParser
{
    public static Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> ParseFileEntry(Span<byte> src, OtZstd zstd)
    {
        using var decompressed = RentedBuffer<byte>.Allocate(OtZstd.GetDecompressedSize(src));
        zstd.Decompress(src, decompressed.Span);

        var table = Byml.FromBinary(decompressed.Span)
            .GetMap(); 
        
        var result = table["BinaryDict"]
            .GetMap()
            .Select(kvp => (kvp.Key, Value: kvp.Value.GetString()))
            .Where(kvp => kvp.Value.Length > 11 && kvp.Value[^11..^4] is "Product")
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        result["static"] = table["StaticEsetb"]
            .GetArray()[0]
            .GetString();

        return result.GetAlternateLookup<ReadOnlySpan<char>>();
    }
}