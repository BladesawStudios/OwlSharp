using Revrs;
using Revrs.Extensions;
using SarcSharp;
using OwlSharp.Core;
using OwlSharp.Core.IO.Buffers;
using OwlSharp.Core.Models;
using OwlSharp.Merging.ResourceSizeTable;

namespace OwlSharp.Merging.Mergers;

public sealed class SarcMerger(TkMerger masterMerger, TkResourceSizeCollector resourceSizeCollector) : ITkMerger
{
    private const ulong DELETED_MARK = 0x44564D5243534B54;
    private readonly TkMerger _masterMerger = masterMerger;
    private readonly TkResourceSizeCollector _resourceSizeCollector = resourceSizeCollector;

    public MergeResult Merge(TkChangelogEntry entry, RentedBuffers<byte> inputs, ArraySegment<byte> vanillaData, Stream output)
    {
        var merged = SarcFile.FromBinary(vanillaData);
        var changelogs = new SarcFile[inputs.Count];

        for (var i = 0; i < inputs.Count; i++) {
            var input = inputs[i];
            changelogs[i] = SarcFile.FromBinary(input.Segment);
        }
        
        MergeMany(entry.Canonical, merged, changelogs);
        merged.Write(output);
        
        return MergeResult.Default;
    }

    public MergeResult Merge(TkChangelogEntry entry, IEnumerable<ArraySegment<byte>> inputs, ArraySegment<byte> vanillaData, Stream output)
    {
        var merged = SarcFile.FromBinary(vanillaData);
        MergeMany(entry.Canonical, merged, inputs.Select(input => SarcFile.FromBinary(input)));
        merged.Write(output);
        
        return MergeResult.Default;
    }

    public MergeResult MergeSingle(TkChangelogEntry entry, ArraySegment<byte> input, ArraySegment<byte> @base, Stream output)
    {
        var merged = SarcFile.FromBinary(@base);
        var changelog = SarcFile.FromBinary(input);
        MergeSingle(entry.Canonical, merged, changelog);
        merged.Write(output);
        
        return MergeResult.Default;
    }

    private void MergeMany(string parentCanonical, SarcFile merged, IEnumerable<SarcFile> changelogs)
    {
        IEnumerable<(string Name, ArraySegment<byte>[] Buffers)> groups = changelogs
            .SelectMany(x => x.Entries)
            .GroupBy(x => x.Name, x => new ArraySegment<byte>(x.Data))
            .Select(x => (x.Key, x.ToArray()));
        
        TkChangelogEntry fakeEntry = new(
            string.Empty,
            ChangelogEntryType.Changelog,
            TkFileAttributes.None,
            zsDictionaryId: -1);
        
        foreach (var (name, buffers) in groups) {
            // Legacy SARC changelogs may contain deletion marks from older TKMM versions.
            // Ignore them so files added by game updates are not removed.
            var effective = buffers.Where(buffer => !IsRemovedEntry(buffer)).ToArray();
            if (effective.Length == 0) {
                continue;
            }

            var last = effective[^1];
            
            if (!merged.TryGet(name, out var vanillaEntry)) {
                if (effective.Length == 1 || _masterMerger.GetMerger(name) is not { } customMerger) {
                    merged.Set(name, last.ToArray());
                    continue;
                }

                fakeEntry.Canonical = $"{parentCanonical}/{name}";
                using Stream customOutput = merged.OpenWrite(name);

                var later = effective.AsSpan(1..);
                var sizes = new int[later.Length];
                for (var i = 0; i < later.Length; i++) {
                    sizes[i] = later[i].Count;
                }

                using var targetsBuffer = RentedBuffers<byte>.Allocate(sizes);
                for (var i = 0; i < later.Length; i++) {
                    later[i].AsSpan().CopyTo(targetsBuffer[i].Span);
                }

                var deltas = TkChangelogBuilder.CreateChangelogsExternal(
                    fakeEntry.Canonical, flags: default, effective[0], targetsBuffer,
                    TkFileAttributes.None, _masterMerger.GameVersion);

                if (deltas.Count == 0) {
                    customOutput.Write(effective[0]);
                }
                else {
                    customMerger.Merge(fakeEntry, deltas, effective[0], customOutput);
                }

                continue;
            }

            if (_masterMerger.GetMerger(name) is not { } merger) {
                merged.Set(name, last.ToArray());
                continue;
            }
            
            fakeEntry.Canonical = $"{parentCanonical}/{name}";
            
            if (effective.Length == 1) {
                using Stream output = merged.OpenWrite(name);
                merger.MergeSingle(fakeEntry, effective[0], vanillaEntry.Data, output);
                continue;
            }

            using (Stream output = merged.OpenWrite(name)) {
                merger.Merge(fakeEntry, effective, vanillaEntry.Data, output);
            }
        }
    }

    private void MergeSingle(string parentCanonical, SarcFile merged, SarcFile changelog)
    {
        TkChangelogEntry fakeEntry = new(
            string.Empty,
            ChangelogEntryType.Changelog,
            TkFileAttributes.None,
            zsDictionaryId: -1);
        
        foreach (var (name, data, _) in changelog.Entries) {
            // Legacy SARC changelogs may contain deletion marks from older TKMM versions.
            // Ignore them so files added by game updates are not removed.
            if (IsRemovedEntry(data)) {
                continue;
            }

            if (!merged.TryGet(name, out var vanillaEntry)) {
                merged.Set(name, data);
                continue;
            }

            if (_masterMerger.GetMerger(name) is not { } merger) {
                merged.Set(name, data);
                continue;
            }

            fakeEntry.Canonical = $"{parentCanonical}/{name}";

            using Stream output = merged.OpenWrite(name);
            merger.MergeSingle(fakeEntry, data, vanillaEntry.Data, output);
        }
    }

    private static bool IsRemovedEntry(ReadOnlySpan<byte> data)
    {
        return data.Length == 8 && data.Read<ulong>() == DELETED_MARK;
    }
}