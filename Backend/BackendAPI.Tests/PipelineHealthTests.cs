using BackendAPI.Observability;
using Xunit;

namespace BackendAPI.Tests;

public sealed class PipelineHealthTests
{
    [Fact]
    public void Health_distinguishes_states_and_only_exposes_sanitized_reason_codes()
    {
        var health=new PipelineRuntimeHealth();
        health.RecordIngestion("youtube",ProviderOperationalState.Misconfigured,0,false,reason:"missing_api_key");
        health.RecordGeneration("openai",false,true,"rate_limited",DateTimeOffset.UtcNow.AddMinutes(1));
        Assert.Equal(ProviderOperationalState.Misconfigured,health.Ingestion.Single().State);
        Assert.Equal("missing_api_key",health.Ingestion.Single().Reason);
        Assert.Equal(ProviderOperationalState.CoolingDown,health.GetGeneration("openai",true,true).State);
        Assert.Equal(ProviderOperationalState.Disabled,health.GetGeneration("custom",false,true).State);
        Assert.Equal(ProviderOperationalState.Misconfigured,health.GetGeneration("anthropic",true,false).State);
    }
}
