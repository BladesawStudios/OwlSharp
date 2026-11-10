using OwlSharp.Core;
using OwlSharp.DevTools.Components;
using OwlSharp.DevTools.Helpers;
using OwlSharp.Extensions.GameBanana.Readers;

namespace OwlSharp.DevTools;

public static class TkApp
{
    static TkApp()
    {
        TkLog.Instance.Register(new ConsoleLogger());
        ReaderProvider.Register(new GameBananaModReader(ReaderProvider));
    }

    public static ITkRomProvider TkRomProvider => RomHelper.Instance;

    public static readonly TkModManager ModManager = TkModManager.CreatePortable();

    public static readonly TkModReaderProvider ReaderProvider = new(ModManager, TkRomProvider);
}