using System.Net;
using System.Text;
using BackendAPI.Interfaces;
using BackendAPI.Services.Llm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BackendAPI.Tests;

public class LlmProviderContractTests
{
    [Theory]
    [InlineData("openai")]
    [InlineData("anthropic")]
    [InlineData("custom")]
    public async Task Every_adapter_transmits_the_common_prompt_and_schema(string providerName)
    {
        const string canonical = "{\"grounding\":0.9}";
        var response = providerName switch
        {
            "openai" => "{\"choices\":[{\"message\":{\"content\":\"{\\\"grounding\\\":0.9}\"}}]}",
            "anthropic" => "{\"content\":[{\"text\":\"{\\\"grounding\\\":0.9}\"}]}",
            _ => canonical
        };
        var handler = new RecordingHandler(response);
        var factory = new FakeHttpClientFactory(handler);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PollGen:OpenAI:ApiKey"] = "test", ["PollGen:OpenAI:Model"] = "test-model",
            ["PollGen:OpenAI:BaseUrl"] = "https://provider.test/v1",
            ["PollGen:Anthropic:ApiKey"] = "test", ["PollGen:Anthropic:Model"] = "test-model",
            ["PollGen:Custom:BaseUrl"] = "https://provider.test"
        }).Build();
        ILlmProvider provider = providerName switch
        {
            "openai" => new OpenAiLlmProvider(factory, config, NullLogger<OpenAiLlmProvider>.Instance),
            "anthropic" => new AnthropicLlmProvider(factory, config, NullLogger<AnthropicLlmProvider>.Instance),
            _ => new CustomVmLlmProvider(factory, config, NullLogger<CustomVmLlmProvider>.Instance)
        };
        var request = new LlmGenerationRequest("COMMON_SYSTEM", "COMMON_PROMPT", "{\"type\":\"object\"}");

        Assert.Equal(canonical, await provider.CompleteAsync(request));
        Assert.Contains("COMMON_SYSTEM", handler.Body);
        Assert.Contains("COMMON_PROMPT", handler.Body);
        Assert.Contains("type", handler.Body);
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    { public HttpClient CreateClient(string name) => new(handler, false); }

    private sealed class RecordingHandler(string response) : HttpMessageHandler
    {
        public string Body { get; private set; } = string.Empty;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response, Encoding.UTF8, "application/json") };
        }
    }
}
