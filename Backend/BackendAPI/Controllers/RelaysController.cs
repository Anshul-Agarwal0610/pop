using System.Security.Claims;
using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Repository;
using BackendAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendAPI.Controllers;

[ApiController]
[Route("api/relays")]
public sealed class RelaysController(IRelayRepository relays, IRewardService rewards, ISystemClock clock) : ControllerBase
{
    [HttpPost, Authorize]
    public Task<IActionResult> Start([FromBody] StartRelayRequest request) => Run(async userId => Ok(await relays.StartAsync(userId, request, clock.UtcNow)));

    [HttpGet("handoffs/{token}")]
    public async Task<IActionResult> Handoff(string token)
    {
        var result=await relays.GetHandoffAsync(token,TryUserId(),clock.UtcNow);
        return result is null?NotFound(new{code=RelayErrorCodes.Replayed,message="Handoff not found."}):Ok(result);
    }

    [HttpPost("handoffs/{token}/accept"),Authorize]
    public Task<IActionResult> Accept(string token) => Run(async userId=>{await relays.AcceptAsync(token,userId,clock.UtcNow);return NoContent();});

    [HttpPost("handoffs/{token}/complete"),Authorize]
    public Task<IActionResult> Complete(string token,[FromBody] CompleteRelayRequest request) => Run(async userId=>
    {
        var result=await relays.CompleteAsync(token,userId,request,clock.UtcNow);
        // Reward only the completed recipient action. The ledger source makes retries idempotent.
        try
        {
            if (result.RewardEligible)
            {
                await rewards.GrantAsync(new(userId,RewardRuleCodes.RelayCompleted,"relay-transfer",$"relay-transfer:{result.ChainId}:{result.ChainLength}",clock.UtcNow));
                if (result.ChainLength is 3 or 5 or 10 or 25 or 50 or 100)
                    await rewards.GrantAsync(new(userId,RewardRuleCodes.RelayMilestone,"relay-milestone",$"relay-milestone:{result.ChainId}:{result.ChainLength}",clock.UtcNow));
            }
        }
        catch (RewardLimitExceededException)
        {
            // A cap affects XP only; the legitimate transfer remains committed.
            result = result with { RewardCapped = true };
        }
        return Ok(result);
    });

    [HttpGet("{chainId:long}"),Authorize]
    public Task<IActionResult> Progress(long chainId)=>Run(async userId=>{var result=await relays.GetProgressAsync(chainId,userId,clock.UtcNow);return result is null?NotFound(new{code=RelayErrorCodes.Forbidden,message="Relay not found."}):Ok(result);});

    [HttpPut("{chainId:long}/outcome-consent"),Authorize]
    public Task<IActionResult> Consent(long chainId,[FromBody] RelayConsentRequest request)=>Run(async userId=>{await relays.SetConsentAsync(chainId,userId,request.ReceiveFinalOutcome);return NoContent();});

    [HttpGet("{chainId:long}/outcome"),Authorize]
    public Task<IActionResult> Outcome(long chainId)=>Run(async userId=>{var result=await relays.GetOutcomeAsync(chainId,userId);return result is null?StatusCode(403,new{code=RelayErrorCodes.Forbidden,message="The final outcome is unavailable or you did not opt in."}):Ok(result);});

    private long? TryUserId()=>long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)??User.FindFirstValue("sub"),out var id)?id:null;
    private async Task<IActionResult> Run(Func<long,Task<IActionResult>> action)
    {
        var userId=TryUserId();if(userId is null)return Unauthorized(new{code="unauthorized",message="Invalid token."});
        try{return await action(userId.Value);}
        catch(RelayDomainException ex){return ex.Code switch{RelayErrorCodes.Expired=>Conflict(new{code=ex.Code,message=ex.Message}),RelayErrorCodes.Forbidden=>StatusCode(403,new{code=ex.Code,message=ex.Message}),_=>Conflict(new{code=ex.Code,message=ex.Message})};}
        catch(ArgumentOutOfRangeException ex){return BadRequest(new{code=RelayErrorCodes.Invalid,message=ex.Message});}
    }
}
