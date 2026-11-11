using System.Collections.Frozen;
using System.IO.Hashing;
using System.Runtime.InteropServices.Marshalling;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using Revrs;
using SarcSharp;
using OwlSharp.Core.IO.Buffers;
using ZstdSharp;

namespace OwlSharp.Core.IO.Caching;

public sealed class OtPackFileLookup(Stream pkcache) 
{
    private static readonly Decompressor _zstd = new();

    private readonly FrozenDictionary<ulong, (string, TkFileAttributes)> _lookup = CreateLookup(pkcache);

    public RentedBuffer<byte> GetNested(string canonical, ITkRom rom, out bool isFoundMissing)
    {
        isFoundMissing = false;

        if (!_lookup.TryGetValue(GetKey(canonical), out var packFile)) {
            return default;
        }

        var (packFileCanonical, attributes) = packFile;
        var sarcBuffer = rom.GetVanilla(packFileCanonical, attributes);

        if (!sarcBuffer.IsEmpty && SarcFile.TryFind(sarcBuffer.Span, canonical, out Range range)) {
            sarcBuffer.Slice(range.Start.Value, range.End.Value);
            return sarcBuffer;
        }
        
        sarcBuffer.Dispose();
        isFoundMissing = true;
        return default;
    }
    
    private static unsafe FrozenDictionary<ulong, (string, TkFileAttributes)> CreateLookup(Stream pkcache)
    {
        const uint magic = 0x48434B50;
        
        using var compressed = RentedBuffer<byte>.Allocate(pkcache);
        using var buffer = RentedBuffer<byte>.Allocate(
            OtZstd.GetDecompressedSize(compressed.Span)
        );
        
        _zstd.Unwrap(compressed.Span, buffer.Span);
        
        var reader = RevrsReader.Native(buffer.Span);
        if (reader.Read<uint>() != magic) {
            throw new InvalidDataException("Invalid pkcache magic");
        }

        var count = reader.Read<int>();
        var stringTableOffset = reader.Read<int>();
        var stringCount = reader.Read<int>();
        
        using var valuesOwner = SpanOwner<(string, TkFileAttributes)>.Allocate(stringCount);
        var values = valuesOwner.Span;

        reader.Seek(stringTableOffset);
        fixed (byte* ptr = &reader.Data[reader.Position]) {
            var pos = ptr;
            for (var i = 0; i < stringCount; i++, pos++) {
                var str = Utf8StringMarshaller.ConvertToManaged(pos)
                          ?? throw new InvalidDataException($"Invalid string in string table at: {i}");
                pos += str.Length + 1;
                values[i] = (str, (TkFileAttributes)(*pos));
            }
        }
        
        reader.Seek(0x10);

        Dictionary<ulong, (string, TkFileAttributes)> result = [];

        for (var i = 0; i < count; i++) {
            var sectionKey = reader.Read<ushort>();
            var hash = reader.Read<uint>();
            var index = reader.Read<ushort>();
            result.Add(GetKey(sectionKey, hash), values[index]);
        }

        return result.ToFrozenDictionary();
    }

    private static ulong GetKey(ReadOnlySpan<char> key)
    {
        var sectionKey = (ushort)((byte)key[0] << 8 | (byte)key[^1]);
        var hash = XxHash32.HashToUInt32(key.Cast<char, byte>());
        return GetKey(sectionKey, hash);
    }

    private static ulong GetKey(ushort sectionKey, uint hash)
    {
        return (ulong)hash << 32 | sectionKey;
    }
}