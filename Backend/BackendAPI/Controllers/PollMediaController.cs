using BackendAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendAPI.Controllers;

[ApiController]
[Route("api/poll-media")]
public sealed class PollMediaController(PollMediaService media) : ControllerBase
{
    [HttpPost]
    [Authorize]
    [RequestSizeLimit(80_000_000)]
    public async Task<IActionResult> Upload([FromForm] IFormFile? file, CancellationToken token)
    {
        if (file is null) return BadRequest(new { message = "Choose an image or video." });
        try { var result = await media.UploadAsync(file, token); return Ok(new { url = result.Url, mediaType = result.MediaType }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return StatusCode(503, new { message = ex.Message }); }
    }

    [HttpGet("{name}")]
    [AllowAnonymous]
    public async Task<IActionResult> Download(string name, CancellationToken token)
    {
        var file = await media.OpenAsync(name, token);
        return file is null ? NotFound() : File(file.Value.Content, file.Value.ContentType, enableRangeProcessing: true);
    }
}
