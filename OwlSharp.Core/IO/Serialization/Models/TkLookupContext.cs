using OwlSharp.Core.Models;

namespace OwlSharp.Core.IO.Serialization.Models;

public class TkLookupContext()
{
    public Dictionary<TkMod, int> Mods { get; } = [];

    public Dictionary<TkModOptionGroup, int> Groups { get; } = [];

    public Dictionary<TkModOption, int> Options { get; } = [];
}