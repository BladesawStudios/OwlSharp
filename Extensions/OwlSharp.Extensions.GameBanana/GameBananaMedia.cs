using System.Text.Json.Serialization;

namespace OwlSharp.Extensions.GameBanana;

public class GameBananaMedia
{
    [JsonPropertyName("_aImages")]
    public List<GameBananaImage> Images { get; set; } = [];
}
