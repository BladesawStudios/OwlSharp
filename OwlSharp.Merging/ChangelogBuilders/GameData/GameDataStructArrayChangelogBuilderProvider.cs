using System.Runtime.CompilerServices;
using OwlSharp.Merging.ChangelogBuilders.BinaryYaml;

namespace OwlSharp.Merging.ChangelogBuilders.GameData;

public class GameDataStructArrayChangelogBuilderProvider : Singleton<GameDataStructArrayChangelogBuilderProvider>, IBymlArrayChangelogBuilderProvider
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IBymlArrayChangelogBuilder GetChangelogBuilder(ref BymlTrackingInfo info, ReadOnlySpan<char> key)
    {
        return key switch {
            "DefaultValue" => new BymlKeyedArrayChangelogBuilder("Hash"),
            _ => BymlArrayChangelogBuilder.Instance
        };
    }
}