using BackendAPI.Services;
using Xunit;

namespace BackendAPI.Tests;

public class GeneratedPollContractTests
{
    [Fact]
    public void AcceptsCanonicalOrderedSides() =>
        Assert.True(GeneratedPollContract.TryValidate(new[] { "Up", "Against" }, out _));

    [Theory]
    [InlineData()]
    [InlineData("Up")]
    [InlineData("Up", "Against", "Other")]
    [InlineData("Up", "Up")]
    [InlineData("Against", "Up")]
    [InlineData("Yes", "No")]
    [InlineData("Support", "Oppose")]
    [InlineData(" up", "Against")]
    [InlineData("up", "against")]
    public void RejectsNonCanonicalShapes(params string[] options) =>
        Assert.False(GeneratedPollContract.TryValidate(options, out _));

    [Fact]
    public void RejectsMissingOptions() =>
        Assert.False(GeneratedPollContract.TryValidate(null, out _));
}
