using OwlSharp.Core;
using OwlSharp.Core.IO;
using OwlSharp.Core.IO.Caching;
using OwlSharp.Data.Embedded;
using Config = OwlSharp.DevTools.ViewModels.SettingsPageViewModel;

namespace OwlSharp.DevTools.Helpers;

public class RomHelper : ITkRomProvider
{
    private static readonly TkChecksums _checksums = TkChecksums.FromStream(TkEmbeddedDataSource.GetChecksumsBin());
    private static readonly OtPackFileLookup _packFileLookup = new(TkEmbeddedDataSource.GetPackFileLookup());
    
    public static readonly RomHelper Instance = new();

    public ITkRom GetRom()
    {
        if (Config.Shared.GameDumpFolderPath is not string gamePath || string.IsNullOrWhiteSpace(gamePath)) {
            throw new InvalidOperationException("Game dump not configured");
        }

        if (!Directory.Exists(gamePath)) {
            throw new DirectoryNotFoundException($"The game dump folder '{gamePath}' was not found");
        }
        
        return new ExtractedTkRom(gamePath, _checksums, _packFileLookup);
    }

}
