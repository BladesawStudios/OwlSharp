using OwlSharp.Merging.Common.BinaryYaml;

namespace OwlSharp.Merging.Mergers.BinaryYaml;

public interface IBymlMergerKeyNameProvider
{
    BymlKeyName GetKeyName(ReadOnlySpan<char> key, ReadOnlySpan<char> type, int depth);
}