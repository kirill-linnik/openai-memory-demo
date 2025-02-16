using System.Text.Json.Serialization;

namespace Backend.Models;

public class User
{
    [property: JsonPropertyName("name")]
    public required string Name { get; set; }
    [property: JsonPropertyName("profile")]
    public string? Profile { get; set; }
}
