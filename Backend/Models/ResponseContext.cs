using Backend.Models.Files;
using System.Text.Json.Serialization;

namespace Backend.Models;

public record ResponseContext(
    [property: JsonPropertyName("dataPoints")] IEnumerable<SupportingContentRecord> DataPointsContent,
    [property: JsonPropertyName("thoughts")] string Thoughts)
{
}
