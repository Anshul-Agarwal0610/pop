using BackendAPI.Interfaces;
using BackendAPI.Jobs;
using BackendAPI.Services.Llm;
using Hangfire;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace BackendAPI.Tests;

public class PollGenerationResilienceTests
{
    [Fact]
    public void RetryDelay_IsCappedAndJitterIsBounded()
    {
        var options = Options.Create(new PollGenerationOptions
        { BaseRetryDelaySeconds=10,MaxRetryDelaySeconds=40,JitterPercentage=.25 });
        var now = new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero);
        var policy = new RetryDelayPolicy(options, new FixedJitter(1));
        Assert.Equal(now.AddSeconds(50), policy.GetNextAttempt(20, now));
    }

    [Fact]
    public void RetryDelay_HonorsLaterProviderReset()
    {
        var options = Options.Create(new PollGenerationOptions
        { BaseRetryDelaySeconds=10,MaxRetryDelaySeconds=100,JitterPercentage=0 });
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        Assert.Equal(now.AddSeconds(80), new RetryDelayPolicy(options,new FixedJitter(0))
            .GetNextAttempt(1,now,now.AddSeconds(80)));
    }

    [Theory]
    [InlineData(429,LlmFailureClass.RateLimited,true)]
    [InlineData(500,LlmFailureClass.TransientServer,true)]
    [InlineData(502,LlmFailureClass.TransientServer,true)]
    [InlineData(503,LlmFailureClass.TransientServer,true)]
    [InlineData(504,LlmFailureClass.TransientServer,true)]
    [InlineData(401,LlmFailureClass.Authentication,false)]
    [InlineData(403,LlmFailureClass.Authentication,false)]
    [InlineData(400,LlmFailureClass.InvalidRequest,false)]
    public void HttpFailures_AreClassified(int status, LlmFailureClass expected, bool retryable)
    {
        var actual = LlmHttpFailureClassifier.Classify((HttpStatusCode)status, "{}");
        Assert.Equal(expected,actual);
        Assert.Equal(retryable,LlmProviderResult.Failure("test",actual).IsRetryable);
    }

    [Fact]
    public void RetryAfter_UsesLatestProviderHeader()
    {
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        using var response = new HttpResponseMessage((HttpStatusCode)429);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(10));
        response.Headers.Add("x-ratelimit-reset-requests","30");
        Assert.Equal(now.AddSeconds(30),LlmHttpFailureClassifier.GetRetryAt(response,now,TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public async Task ProviderTimeout_IsRetryable()
    {
        using var client = new HttpClient(new ThrowingHandler(new TaskCanceledException("timeout")));
        var result = await LlmProviderHttp.SendAsync("test",client,new HttpRequestMessage(HttpMethod.Get,"https://example.test"),
            x=>x,NullLogger.Instance,TimeSpan.FromMinutes(1),CancellationToken.None);
        Assert.Equal(LlmFailureClass.Timeout,result.FailureClass);
        Assert.True(result.IsRetryable);
    }

    [Fact]
    public async Task NetworkFailure_IsRetryable()
    {
        using var client = new HttpClient(new ThrowingHandler(new HttpRequestException("network")));
        var result = await LlmProviderHttp.SendAsync("test",client,new HttpRequestMessage(HttpMethod.Get,"https://example.test"),
            x=>x,NullLogger.Instance,TimeSpan.FromMinutes(1),CancellationToken.None);
        Assert.Equal(LlmFailureClass.TransientServer,result.FailureClass);
    }

    [Fact]
    public void ContentPolicy_IsTerminal() => Assert.Equal(LlmFailureClass.ContentPolicy,
        LlmHttpFailureClassifier.Classify(HttpStatusCode.BadRequest,"{\"code\":\"content_policy_violation\"}"));

    [Fact]
    public void HangfireAutomaticRetry_IsDisabled()
    {
        var attribute = typeof(PollGenerationJob).GetMethod(nameof(PollGenerationJob.RunAsync))!
            .GetCustomAttributes(typeof(AutomaticRetryAttribute),false).Cast<AutomaticRetryAttribute>().Single();
        Assert.Equal(0,attribute.Attempts);
    }

    private sealed class FixedJitter(double value) : IJitterSource { public double Next() => value; }
    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(exception);
    }
}
