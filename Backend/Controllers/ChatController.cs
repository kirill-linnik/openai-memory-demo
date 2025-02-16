using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("chat")]
public class ChatController(ChatCompletionService chatCompletionService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ProcessMessageAsync([FromBody] ChatRequest chatRequest, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chatRequest, nameof(chatRequest));
        ArgumentNullException.ThrowIfNullOrWhiteSpace(chatRequest.Message, nameof(chatRequest.Message));
        ArgumentNullException.ThrowIfNullOrWhiteSpace(chatRequest.RequestId, nameof(chatRequest.RequestId));

        var response = await chatCompletionService.ProcessRequestAsync(chatRequest, cancellationToken);
        return Ok(response);
    }
}
