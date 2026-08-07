using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace BackendAPI.Controllers;

[ApiController, Route("api/result-cards")]
public sealed class ResultCardsController(IResultCardsRepository repository, ResultCardFactory factory, ISystemClock clock) : ControllerBase
{
    private long? UserId => long.TryParse((User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub"))?.Value, out var id) ? id : null;

    [Authorize, HttpGet("session/{sessionId:long}")]
    public async Task<IActionResult> GetSession(long sessionId)
    {
        if (UserId is not long userId) return Unauthorized();
        var card = await repository.GetForParticipantAsync(sessionId, userId);
        return card is null ? NotFound() : Ok(card);
    }

    [Authorize, HttpGet("me")]
    public async Task<IActionResult> GetMine([FromQuery] int offset = 0, [FromQuery] int limit = 12)
    {
        if (UserId is not long userId) return Unauthorized();
        return Ok(await repository.GetMineAsync(userId, Math.Max(0, offset), Math.Clamp(limit, 1, 50)));
    }

    [AllowAnonymous, HttpGet("public/{token}")]
    public async Task<IActionResult> GetPublic(string token)
    {
        var row = await FindPublic(token);
        if (row.Result is not null) return row.Result;
        var payload = factory.Deserialize(row.Card!.PayloadJson);
        return Ok(new { row.Card.Id, row.Card.PublicToken, Payload = payload,
            PublicUrl = $"{Request.Scheme}://{Request.Host}/live/cards/{row.Card.PublicToken}",
            ImageUrl = $"{Request.Scheme}://{Request.Host}/api/result-cards/public/{row.Card.PublicToken}/image",
            row.Card.CreatedAt, row.Card.ExpiresAt });
    }

    [AllowAnonymous, HttpGet("public/{token}/image")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetImage(string token)
    {
        var row = await FindPublic(token);
        if (row.Result is not null) return row.Result;
        var p = factory.Deserialize(row.Card!.PayloadJson);
        static string E(string value) => WebUtility.HtmlEncode(value);
        var svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="1200" height="630" viewBox="0 0 1200 630" role="img" aria-label="{E(p.AccessibleSummary)}">
              <rect width="1200" height="630" fill="#17122b"/><circle cx="1080" cy="90" r="210" fill="#7c3aed" opacity=".45"/>
              <text x="80" y="110" fill="#c4b5fd" font-family="system-ui,sans-serif" font-size="36" font-weight="700">PoP Live · {E(p.Mode)}</text>
              <text x="80" y="255" fill="white" font-family="system-ui,sans-serif" font-size="64" font-weight="800">{E(p.AggregateResult)}</text>
              <text x="80" y="350" fill="#ddd6fe" font-family="system-ui,sans-serif" font-size="34">{E(p.Milestone ?? $"{p.ParticipantCount} participants")}</text>
              <text x="80" y="510" fill="#a78bfa" font-family="system-ui,sans-serif" font-size="30">{E(p.Badge is null ? "A PoP Live memory" : $"{p.Badge.Icon} {p.Badge.Name}")}</text>
            </svg>
            """;
        Response.Headers.CacheControl = "public,max-age=3600,immutable";
        Response.Headers.Append("Content-Disposition", $"attachment; filename=pop-live-{token[..Math.Min(8, token.Length)]}.svg");
        return File(Encoding.UTF8.GetBytes(svg), "image/svg+xml; charset=utf-8");
    }

    private async Task<(StoredResultCard? Card, IActionResult? Result)> FindPublic(string token)
    {
        if (token.Length is < 32 or > 64 || token.Any(c => !Uri.IsHexDigit(c))) return (null, NotFound());
        var card = await repository.GetPublicAsync(token);
        if (card is null) return (null, NotFound());
        if (card.RevokedAt is not null || card.ExpiresAt <= clock.UtcNow) return (null, StatusCode(StatusCodes.Status410Gone, new { message = "This shared memory is no longer available." }));
        return (card, null);
    }
}
