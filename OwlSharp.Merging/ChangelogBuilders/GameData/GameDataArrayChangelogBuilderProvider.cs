using System.Runtime.CompilerServices;
using OwlSharp.Merging.ChangelogBuilders.BinaryYaml;

namespace OwlSharp.Merging.ChangelogBuilders.GameData;

public class GameDataArrayChangelogBuilderProvider : Singleton<GameDataArrayChangelogBuilderProvider>, IBymlArrayChangelogBuilderProvider
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IBymlArrayChangelogBuilder GetChangelogBuilder(ref BymlTrackingInfo info, ReadOnlySpan<char> key)
    {
        return BymlDirectIndexArrayChangelogBuilder.Instance;
    }
}