using OwlSharp.Core;
using OwlSharp.Core.IO;
using OwlSharp.Core.IO.Caching;
using OwlSharp.Data.Embedded;
using OwlSharp.Extensions.LibHac;
using Config = OwlSharp.DevTools.ViewModels.SettingsPageViewModel;

namespace OwlSharp.DevTools.Helpers;

public class RomHelper : ITkRomProvider
{
    private static readonly TkChecksums _checksums = TkChecksums.FromStream(TkEmbeddedDataSource.GetChecksumsBin());
    private static readonly TkPackFileLookup _packFileLookup = new(TkEmbeddedDataSource.GetPackFileLookup());
    
    public static readonly RomHelper Instance = new();

    public ITkRom GetRom()
    {
        if (Config.Shared.GameDumpFolderPath is string gamePath && Directory.Exists(gamePath)) {
            return new ExtractedTkRom(gamePath, _checksums, _packFileLookup);
        }

        if (Config.Shared.KeysFolderPath is not string keysFolderPath) {
            throw new InvalidOperationException("Keys folder path is required but not configured.");
        }

        if (!File.Exists(Path.Combine(keysFolderPath, "prod.keys"))) {
            throw new FileNotFoundException($"A 'prod.keys' file could not be found in '{keysFolderPath}'");
        }

        if (!File.Exists(Path.Combine(keysFolderPath, "title.keys"))) {
            throw new FileNotFoundException($"A 'title.keys' file could not be found in '{keysFolderPath}'");
        }

        // Base game can come from a packaged file or split files; the update from a packaged file.
        // The SD card can supply either.
        return TkExtensibleRomProviderBuilder.Create(_checksums, _packFileLookup)
            .WithKeysFolder(keysFolderPath)
            .WithPackagedBaseGame(GetPaths(Config.Shared.BaseGameFilePath, Config.Shared.SplitFilesPath))
            .WithPackagedUpdate(GetPaths(Config.Shared.GameUpdateFilePath))
            .WithSdCard(Config.Shared.SdCardRootPath)
            .Build()
            .GetRom();
    }

    private static string[] GetPaths(params string?[] paths)
        => [.. paths.Where(static path => !string.IsNullOrWhiteSpace(path) && Path.Exists(path))!];
}
