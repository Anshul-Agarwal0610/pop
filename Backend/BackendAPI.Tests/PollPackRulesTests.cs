using BackendAPI.Models; using BackendAPI.Repository; using Xunit;
namespace BackendAPI.Tests;
public sealed class PollPackRulesTests
{
 [Fact] public async Task Packs_emit_only_canonical_binary_choices(){var r=new PollPacksRepository();var p=await r.CreateAsync(7,new("Test","",PollPackVisibility.Private,["A proposition"]));Assert.Equal(["Up","Against"],p.Items.Single().Choices);}
 [Fact] public async Task Blank_propositions_are_rejected(){var r=new PollPacksRepository();await Assert.ThrowsAsync<PollPackException>(()=>r.CreateAsync(7,new("Test","",PollPackVisibility.Private,[" "])));}
 [Fact] public async Task Submitted_pack_is_frozen_and_not_public(){var r=new PollPacksRepository();var p=await r.CreateAsync(7,new("Test","",PollPackVisibility.Public,["One"]));await r.SubmitAsync(p.Id,7);await Assert.ThrowsAsync<PollPackException>(()=>r.UpdateAsync(p.Id,7,new("Changed","",PollPackVisibility.Public,["Two"])));Assert.DoesNotContain(await r.PublishedAsync(),x=>x.Id==p.Id);}
}
