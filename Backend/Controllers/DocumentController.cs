using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("documents")]
public class DocumentController(AzureBlobStorageService service, ILogger<DocumentController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> UploadDocumentsAsync([FromForm] IFormFileCollection files, CancellationToken cancellationToken)
    {
        logger.LogInformation("Upload documents");

        var response = await service.UploadDocumentsAsync(files, cancellationToken);

        logger.LogInformation("Upload documents: {x}", response);

        return response.IsSuccessful ? Ok(response) : UnprocessableEntity(response.Error);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetDocumentsAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Get documents");

        var response = await service.GetDocumentsAsync(cancellationToken);

        return Ok(response);
    }
}
