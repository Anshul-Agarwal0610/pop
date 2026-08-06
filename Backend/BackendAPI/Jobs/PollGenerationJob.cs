using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Hangfire;

namespace BackendAPI.Jobs
{
    /// <summary>
    /// Hangfire recurring job that reads unprocessed TrendingTopics,
    /// calls the poll-generation provider for each, persists the resulting poll,
    /// and marks the topic as processed.
    /// </summary>
    public class PollGenerationJob
    {
        private readonly ITrendingTopicRepository _topicRepo;
        private readonly IPollsRepository _pollsRepo;
        private readonly IPollGenerationService _generator;
        private readonly IGeneratedPollQualityGate _qualityGate;
        private readonly ILogger<PollGenerationJob> _logger;

        // Polls expire 48 hours after creation by default.
        private static readonly TimeSpan DefaultExpiry = TimeSpan.FromHours(48);

        // Delay between LLM calls to stay under free-tier rate limits.
        private static readonly TimeSpan LlmDelay = TimeSpan.FromSeconds(13);

        public PollGenerationJob(
            ITrendingTopicRepository topicRepo,
            IPollsRepository pollsRepo,
            IPollGenerationService generator,
            IGeneratedPollQualityGate qualityGate,
            ILogger<PollGenerationJob> logger)
        {
            _topicRepo = topicRepo;
            _pollsRepo = pollsRepo;
            _generator = generator;
            _qualityGate = qualityGate;
            _logger = logger;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 600)]
        public async Task RunAsync()
        {
            _logger.LogInformation("[PollGenerationJob] Starting at {Time}", DateTime.UtcNow);

            // Process 5 topics per run to match free-tier rate limits.
            var topics = (await _topicRepo.GetUnprocessedAsync(maxCount: 5)).ToList();

            if (topics.Count == 0)
            {
                _logger.LogInformation("[PollGenerationJob] No unprocessed topics. Nothing to do.");
                return;
            }

            _logger.LogInformation("[PollGenerationJob] Processing {Count} topics", topics.Count);

            int created = 0, skipped = 0;

            foreach (var topic in topics)
            {
                var generated = await _generator.GenerateAsync(topic);

                if (generated == null)
                {
                    _logger.LogWarning(
                        "[PollGenerationJob] Skipping topic {TopicId}. Generation returned null. SourceType={SourceType}",
                        topic.Id, topic.SourceType);
                    skipped++;
                    await _topicRepo.MarkProcessedAsync(topic.Id);
                    continue;
                }

                await Task.Delay(LlmDelay);

                try
                {
                    var decision = await _qualityGate.EvaluateAsync(topic, generated, GeneratedPollContract.CanonicalOptions);
                    _logger.LogInformation("[PollQuality] TopicId={TopicId} Disposition={Disposition} Reasons={ReasonCodes} Sensitive={Sensitive} Rules={RulesVersion} Schema={SchemaVersion} ScoreBucket={ScoreBucket} DuplicateType={DuplicateType}",
                        topic.Id, decision.Disposition, string.Join(',', decision.ReasonCodes), decision.IsSensitive,
                        decision.RulesVersion, decision.EvaluatorSchemaVersion, Math.Floor(decision.OverallScore * 10) / 10,
                        decision.DuplicateMatchType);
                    if (decision.Disposition == PollQualityDisposition.Rejected)
                    {
                        skipped++;
                        await _pollsRepo.RecordRejectedQualityDecisionAsync(topic.Id, decision);
                        await _topicRepo.MarkProcessedAsync(topic.Id);
                        continue;
                    }
                    var request = new CreatePollRequest
                    {
                        Question = generated.Proposition,
                        Description = topic.Summary,
                        Category = generated.Category,
                        ExpiresAt = DateTime.UtcNow.Add(DefaultExpiry),
                        Options = GeneratedPollContract.CanonicalOptions.ToList(),
                        SourceType = topic.SourceType,
                        SourceUrl = topic.SourceUrl,
                        ThumbnailUrl = topic.ThumbnailUrl,
                        IsAIGenerated = true,
                        QualityDecision = decision,
                        TrendingTopicId = topic.Id,
                        ModerationReason = string.Join(',', decision.ReasonCodes)
                    };

                    var pollId = await _pollsRepo.CreateAsync(request);
                    await _topicRepo.MarkProcessedAsync(topic.Id);

                    _logger.LogInformation(
                        "[PollGenerationJob] Created generated poll {PollId} from topic {TopicId}. Disposition={Disposition} Rules={RulesVersion}",
                        pollId, topic.Id, decision.Disposition, decision.RulesVersion);

                    created++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[PollGenerationJob] Failed to save generated poll for topic {TopicId}", topic.Id);
                }
            }

            _logger.LogInformation(
                "[PollGenerationJob] Done. Created: {Created}, skipped: {Skipped}",
                created,
                skipped);
        }
    }
}
