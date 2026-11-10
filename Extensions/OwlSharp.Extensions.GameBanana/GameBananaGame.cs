using System.Text.Json.Serialization;

namespace OwlSharp.Extensions.GameBanana;

public class GameBananaGame
{
    [JsonPropertyName("_idRow")]
    public int Id { get; set; }
}