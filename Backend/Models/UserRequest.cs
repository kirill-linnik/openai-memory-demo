namespace Backend.Models;

public class UserRequest
{
    public string Content { get; set; } = string.Empty;
    public string? LastAssistantResponse { get; set; }
}
