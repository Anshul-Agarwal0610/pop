using BackendAPI.Interfaces;
using BackendAPI.Repository;
using BackendAPI.Services;

namespace BackendAPI.Jobs;

public sealed class PhaseOneGeneration(PhaseOnePipelineRepository repository, TopicBatchGenerator generator,
    ITrendingTopicRepository topics, IConfiguration config, ILogger<PhaseOneGeneration> logger)
{
    public async Task RunAsync(int? requestedCount=null)
    {
        if ((await topics.GetControlStateAsync()).GenerationPaused) return;
        var count=Math.Min(requestedCount ?? 1,Math.Clamp(config.GetValue("Pipeline:MaxGenerationBatch",1),1,100));
        for(int i=0;i<count;i++)
        {
            var topics = new List<PhaseOneTopic>();
            var topicsPerRequest = Math.Clamp(config.GetValue("PhaseOne:TopicsPerRequest", 10), 1, 10);
            var first = await repository.ClaimAsync();
            if (first is null) break;
            topics.Add(first);
            var language = TopicLanguagePolicy.BatchLanguage(first.Language)
                ?? throw new InvalidOperationException("Claimed topic has an unsupported language.");
            for (var claimed = 1; claimed < topicsPerRequest; claimed++)
            {
                var topic = await repository.ClaimAsync(language);
                if (topic is null) break;
                topics.Add(topic);
            }
            var savedTopics = topics.Where(topic => topic.PhaseOnePayload is not null).ToList();
            foreach (var topic in savedTopics)
            {
                try { var published = await repository.PublishAsync(topic); logger.LogInformation("Phase one topic {TopicId}: recovered {Count} saved polls", topic.Id, published); }
                catch (Exception ex) { logger.LogError(ex, "Phase one topic {TopicId} saved publication failed", topic.Id); await repository.FailAsync(topic, ex.Message, false); }
            }
            topics = topics.Where(topic => topic.PhaseOnePayload is null).ToList();
            if (topics.Count == 0) continue;
            MultiTopicBatchResult result;
            try
            {
                logger.LogInformation("Generating {Count} {Language} topics in one provider request", topics.Count, language);
                result = await generator.GenerateBatchAsync(topics);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Phase one batch generation failed before a provider result was saved");
                foreach (var topic in topics) await repository.FailAsync(topic, ex.Message, true);
                continue;
            }
            if(result.Batch is null)
            {
                foreach (var topic in topics) await repository.FailAsync(topic,result.Provider.Reason ?? "Generation failed",result.Provider.IsRetryable || result.Provider.FailureClass==LlmFailureClass.Configuration,result.Provider.RetryAtUtc);
                continue;
            }
            foreach (var topic in topics)
            {
                try
                {
                    var output = result.Batch.Topics.FirstOrDefault(x => x.TopicId == topic.Id)?.Polls ?? [];
                    await repository.SaveOutputAsync(topic, new TopicBatchResult(result.Provider, new TopicPollBatch(output)));
                    var published=await repository.PublishAsync(topic);
                    logger.LogInformation("Phase one topic {TopicId}: published {Count} polls",topic.Id,published);
                }
                catch(Exception ex)
                {
                    // One publication failure must not strand the other topics in a paid batch.
                    logger.LogError(ex,"Phase one topic {TopicId} publication failed",topic.Id);
                    await repository.FailAsync(topic, ex.Message, false);
                }
            }
        }
    }
}
