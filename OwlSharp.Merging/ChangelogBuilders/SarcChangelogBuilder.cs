using SarcSharp;
using OwlSharp.Core;

namespace OwlSharp.Merging.ChangelogBuilders;

public sealed class SarcChangelogBuilder : Singleton<SarcChangelogBuilder>, ITkChangelogBuilder
{
    public bool CanProcessWithoutVanilla => false;

    public bool Build(string canonical, in TkPath path, in TkChangelogBuilderFlags flags, ArraySegment<byte> srcBuffer,
        ArraySegment<byte> vanillaBuffer, OpenWriteChangelog openWrite, int gameVersion)
    {
        var vanilla = SarcFile.FromBinary(vanillaBuffer);

        SarcFile changelog = new();
        var sarc = SarcFile.FromBinary(srcBuffer);

        foreach (var (name, data, _) in sarc.Entries) {
            if (TkChangelogBuilder.SessionRom.IsVanillaAnyVersion($"{canonical}/{name}", data.AsSpan())) {
                continue;
            }
            
            if (!vanilla.TryGet(name, out var vanillaEntry)) {
                // Custom file, use entire content
                goto MoveContent;
            }
            
            var vanillaData = vanillaEntry.Data;

            if (data.AsSpan().SequenceEqual(vanillaData)) {
                // Vanilla file, ignore
                continue;
            }

            var nested = new TkPath(
                name,
                fileVersion: path.FileVersion,
                TkFileAttributes.None,
                root: "romfs",
                name
            );

            if (TkChangelogBuilder.GetChangelogBuilder(nested) is not { } builder) {
                goto MoveContent;
            }

            if (name.EndsWith("__Combined.bntx")) {
                builder.Build($"{canonical}/{name}", nested, flags, data, vanillaData,
                    (_, _, _, _) => changelog.OpenWrite(name), gameVersion);
            }
            else {
                builder.Build(name, nested, flags, data, vanillaData,
                    (_, canon, _, _) => changelog.OpenWrite(canon), gameVersion);
            }

            builder.Dispose();

            continue;

        MoveContent:
            changelog.Set(name, data);
        }

        if (changelog.Count == 0) {
            return false;
        }

        using var output = openWrite(path, canonical);
        changelog.Write(output);
        return true;
    }

    public void Dispose()
    {
    }
}