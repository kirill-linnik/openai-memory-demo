using System.Text.Json.Serialization;

namespace Backend.Models.Files;

public record SupportingContentRecord(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("content")] string Content);