using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("user-request")]
public class UserRequestController(UserRequestService userRequestService) : ControllerBase
{
    [HttpGet("{requestId}")]
    public IActionResult GetUserRequest([FromRoute] string requestId)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(requestId, nameof(requestId));
        var userRequest = userRequestService.GetUserRequest(requestId);
        return Ok(userRequest);
    }
}
