using OwlSharp.Core.Models;

namespace OwlSharp.Core;

public interface ITkSystemProvider
{
    ITkModWriter GetSystemWriter(TkModContext context, bool enableRomfsBucketing = true);
    
    ITkSystemSource GetSystemSource(string relativeFolderPath);
}