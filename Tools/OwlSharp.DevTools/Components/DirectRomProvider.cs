using OwlSharp.Core;

namespace OwlSharp.DevTools.Components;

public class DirectRomProvider(ITkRom rom) : ITkRomProvider
{
    public ITkRom GetRom()
    {
        return rom;
    }
}