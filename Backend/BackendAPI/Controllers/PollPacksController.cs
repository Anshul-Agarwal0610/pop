using System.Security.Claims; using BackendAPI.Interfaces; using BackendAPI.Models; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc;
namespace BackendAPI.Controllers;
[ApiController,Route("api/poll-packs")] public sealed class PollPacksController(IPollPacksRepository packs):ControllerBase
{
 [AllowAnonymous,HttpGet] public async Task<IActionResult> Published()=>Ok(await packs.PublishedAsync());
 [Authorize,HttpGet("mine")] public async Task<IActionResult> Mine()=>Ok(await packs.MineAsync(UserId()));
 [Authorize,HttpPost] public async Task<IActionResult> Create(SavePollPackRequest q)=>Ok(await packs.CreateAsync(UserId(),q));
 [Authorize,HttpPut("{id:long}")] public async Task<IActionResult> Update(long id,SavePollPackRequest q)=>Ok(await packs.UpdateAsync(id,UserId(),q));
 [Authorize,HttpPost("{id:long}/submit")] public async Task<IActionResult> Submit(long id)=>Ok(await packs.SubmitAsync(id,UserId()));
 [Authorize(Policy="Admin"),HttpPatch("{id:long}/moderation")] public async Task<IActionResult> Moderate(long id,ModeratePollPackRequest q)=>Ok(await packs.ModerateAsync(id,UserId(),q));
 long UserId(){var c=User.FindFirst(ClaimTypes.NameIdentifier)??User.FindFirst("sub");return c is not null&&long.TryParse(c.Value,out var id)?id:throw new UnauthorizedAccessException();}
}
