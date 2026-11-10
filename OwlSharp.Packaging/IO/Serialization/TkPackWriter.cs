using CommunityToolkit.HighPerformance;
using OwlSharp.Core.IO.Serialization;
using OwlSharp.Core.Models;

namespace OwlSharp.Packaging.IO.Serialization;

public static class TkPackWriter
{
    public static void Write(in Stream output, TkMod mod)
    {
        output.Write(TkBinaryWriter.TKPK_MAGIC);
        output.Write(TkBinaryWriter.TKPK_VERSION);
        
        TkBinaryWriter.WriteTkMod(output, mod);
    }
}
