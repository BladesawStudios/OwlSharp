using OwlSharp.Core;
using OwlSharp.Core.Common;
using OwlSharp.Core.IO.Caching;
using OwlSharp.Data.Embedded;
using OwlSharp.Extensions.LibHac;

namespace OwlSharp.Debug;

public sealed class DebugRomProvider : Singleton<DebugRomProvider>, ITkRomProvider, IDisposable
{
    private TkPackFileLookup? _packFileLookup;

    public ITkRom GetRom()
    {
        // return new ExtractedTkRom(@"F:\Games\RomFS\Totk\1.2.1",
        //     TkChecksums.FromStream(TkEmbeddedDataSource.GetChecksumsBin()),
        //     new TkPackFileLookup(TkEmbeddedDataSource.GetPackFileLookup())
        // );

        _packFileLookup ??= new TkPackFileLookup(TkEmbeddedDataSource.GetPackFileLookup());

        return TkExtensibleRomProviderBuilder
            .Create(TkChecksums.FromStream(TkEmbeddedDataSource.GetChecksumsBin()), _packFileLookup)
            .WithKeysFolder(@"F:\TOTK\SDCardTest\switch")
            .WithPackagedBaseGame([@"F:\TOTK\SDCardTest\TOTKSPLIT"])
            .WithSdCard(@"F:\TOTK\SDCardTest")
            .Build()
            .GetRom();
    }

    public void Dispose()
    {
        _packFileLookup?.Dispose();
        _packFileLookup = null;
    }
}
