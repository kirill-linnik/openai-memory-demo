using System.Text.Json.Serialization;

namespace Backend.Models;

public record ResponseChoice(
    [property: JsonPropertyName("message")] ResponseMessage Message,
    [property: JsonPropertyName("context")] ResponseContext Context)
{ }
