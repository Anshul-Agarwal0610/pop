using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using Xunit;

namespace BackendAPI.Tests;

public class LlmProviderContractTests
{
    [Theory]
    [InlineData(408, LlmProviderOutcome.TransientFailure)]
    [InlineData(429, LlmProviderOutcome.TransientFailure)]
    [InlineData(503, LlmProviderOutcome.TransientFailure)]
    [InlineData(401, LlmProviderOutcome.PermanentFailure)]
    [InlineData(400, LlmProviderOutcome.PermanentFailure)]
    public async Task OpenAi_classifies_http_failures(int status, LlmProviderOutcome expected)
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage((HttpStatusCode)status));
        var provider = new OpenAiLlmProvider(new TestClientFactory(handler), Monitor(OptionsFor("openai")), NullLogger<OpenAiLlmProvider>.Instance);
        var result = await provider.GenerateAsync(Request());
        Assert.Equal(expected, result.Outcome);
        Assert.Equal("openai", result.Provider);
        Assert.Equal("test-model", result.Model);
        Assert.Equal("Bearer", handler.Request!.Headers.Authorization!.Scheme);
        Assert.DoesNotContain("secret-key", result.Reason ?? "");
    }

    [Fact]
    public async Task Groq_uses_compatible_wire_contract_but_own_identity()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK, "{\"choices\":[{\"message\":{\"content\":\"{\\\"question\\\":\\\"A sufficiently long question for publishing?\\\",\\\"options\\\":[\\\"Up\\\",\\\"Against\\\"]}\"}}]}"));
        var provider = new GroqLlmProvider(new TestClientFactory(handler), Monitor(OptionsFor("groq")), NullLogger<GroqLlmProvider>.Instance);
        var result = await provider.GenerateAsync(Request());
        Assert.Equal(LlmProviderOutcome.Success, result.Outcome);
        Assert.Equal("groq", result.Provider);
        Assert.Contains("test-model", handler.RequestBody);
    }

    [Fact]
    public async Task Anthropic_maps_native_envelope_and_headers()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK, "{\"content\":[{\"text\":\"{}\"}]}"));
        var provider = new AnthropicLlmProvider(new TestClientFactory(handler), Monitor(OptionsFor("anthropic")));
        var result = await provider.GenerateAsync(Request());
        Assert.Equal(LlmProviderOutcome.Success, result.Outcome);
        Assert.True(handler.Request!.Headers.Contains("x-api-key"));
        Assert.True(handler.Request.Headers.Contains("anthropic-version"));
    }

    [Fact]
    public async Task Gemini_maps_native_envelope_and_endpoint_key()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK, "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"{}\"}]}}]}"));
        var provider = new GeminiLlmProvider(new TestClientFactory(handler), Monitor(OptionsFor("gemini")));
        var result = await provider.GenerateAsync(Request());
        Assert.Equal(LlmProviderOutcome.Success, result.Outcome);
        Assert.Equal("gemini", result.Provider);
        Assert.True(handler.Request!.Headers.Contains("x-goog-api-key"));
    }

    [Fact]
    public async Task Caller_cancellation_is_propagated()
    {
        var handler = new RecordingHandler(async (_, ct) => { await Task.Delay(Timeout.Infinite, ct); return new HttpResponseMessage(HttpStatusCode.OK); });
        var provider = new OpenAiLlmProvider(new TestClientFactory(handler), Monitor(OptionsFor("openai")), NullLogger<OpenAiLlmProvider>.Instance);
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GenerateAsync(Request(), cts.Token));
    }

    private static LlmGenerationRequest Request() => new("system", "prompt");
    private static PollGenerationOptions OptionsFor(string name) => new() { ProviderOrder = [name], Providers = new(StringComparer.OrdinalIgnoreCase) { [name] = new() { Enabled = true, Model = "test-model", Endpoint = "https://example.test/generate", TimeoutSeconds = 5, ApiKey = "secret-key" } } };
    private static IOptionsMonitor<PollGenerationOptions> Monitor(PollGenerationOptions value) => new TestMonitor<PollGenerationOptions>(value);
    private static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class TestClientFactory(HttpMessageHandler handler) : IHttpClientFactory { public HttpClient CreateClient(string name) => new(handler, false); }
    private sealed class TestMonitor<T>(T value) : IOptionsMonitor<T> { public T CurrentValue => value; public T Get(string? name) => value; public IDisposable? OnChange(Action<T, string?> listener) => null; }
    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;
        public HttpRequestMessage? Request { get; private set; }
        public string RequestBody { get; private set; } = string.Empty;
        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : this((r, _) => Task.FromResult(send(r))) { }
        public RecordingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) => _send = send;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) { Request = request; RequestBody = request.Content?.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult() ?? string.Empty; return _send(request, cancellationToken); }
    }
}
