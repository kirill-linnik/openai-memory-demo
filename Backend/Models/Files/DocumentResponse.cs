namespace Backend.Models.Files;

public record class DocumentResponse(
    string Name,
    string ContentType,
    long Size,
    DateTimeOffset? LastModified,
    DocumentProcessingStatus Status);