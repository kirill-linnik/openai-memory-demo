using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("user")]
public class UserController(UserService userService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetUser()
    {
        var user = userService.GetUser();
        return Ok(user);
    }
}
