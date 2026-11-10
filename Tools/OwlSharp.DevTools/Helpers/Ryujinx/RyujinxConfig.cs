using System.Text.Json.Serialization;

namespace OwlSharp.DevTools.Helpers.Ryujinx;

public record RyujinxConfig(
    [property: JsonPropertyName("game_dirs")] List<string> GameDirs
);