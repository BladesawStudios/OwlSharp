
using OwlSharp.Core;
using OwlSharp.Core.Common;
using OwlSharp.Core.IO;
using OwlSharp.Core.IO.Caching;
using OwlSharp.Data.Embedded;

namespace OwlSharp.Debug;

public sealed class DebugRomProvider : Singleton<DebugRomProvider>, ITkRomProvider, IDisposable {
    private const string GAME_DUMP_PATH = @"E:\Nintendo Stuff\ROMS\Switch\Tears Dump";
    
    private TkPackFileLookup? _packFileLookup;

    public ITkRom GetRom()
    {
        _packFileLookup ??= new TkPackFileLookup(TkEmbeddedDataSource.GetPackFileLookup());

        return new ExtractedTkRom(GAME_DUMP_PATH, TkChecksums.FromStream(TkEmbeddedDataSource.GetChecksumsBin()),
            _packFileLookup);
    }

    public void Dispose()
    {
        _packFileLookup?.Dispose();
        _packFileLookup = null;
    }
}