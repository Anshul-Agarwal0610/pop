using BackendAPI.Models;
using Xunit;

namespace BackendAPI.Tests;

public class PollRemixRulesTests
{
    [Theory]
    [InlineData("Should we add car-free streets?", "should we add car-free streets? ", false)]
    [InlineData("Should we add car-free streets?", "Should we add car-free streets downtown?", true)]
    [InlineData("Should we add car-free streets?", "  ", false)]
    public void RequiresAnOriginalQuestion(string source, string candidate, bool expected) =>
        Assert.Equal(expected, PollRemixRules.IsNewQuestion(source, candidate));
}
