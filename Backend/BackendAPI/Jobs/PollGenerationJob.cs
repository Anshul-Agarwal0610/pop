using BackendAPI.Interfaces;
using BackendAPI.Models;

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
        private readonly ILogger<PollGenerationJob> _logger;

        // Polls expire 48 hours after creation by default.
        private static readonly TimeSpan DefaultExpiry = TimeSpan.FromHours(48);

        // Delay between LLM calls to stay under free-tier rate limits.
        private static readonly TimeSpan LlmDelay = TimeSpan.FromSeconds(13);

        public PollGenerationJob(
            ITrendingTopicRepository topicRepo,
            IPollsRepository pollsRepo,
            IPollGenerationService generator,
            ILogger<PollGenerationJob> logger)
        {
            _topicRepo = topicRepo;
            _pollsRepo = pollsRepo;
            _generator = generator;
            _logger = logger;
        }

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
                        "[PollGenerationJob] Skipping topic {TopicId} '{Title}' from {SourceType}. Generation returned null.",
                        topic.Id,
                        topic.Title,
                        topic.SourceType);
                    skipped++;
                    await _topicRepo.MarkProcessedAsync(topic.Id);
                    continue;
                }

                await Task.Delay(LlmDelay);

                try
                {
                    var request = new CreatePollRequest
                    {
                        Question = generated.Proposition,
                        Description = topic.Summary,
                        Category = generated.Category,
                        ExpiresAt = DateTime.UtcNow.Add(DefaultExpiry),
                        Options = new List<string> { "Up", "Against" },
                        SourceType = topic.SourceType,
                        SourceUrl = topic.SourceUrl,
                        ThumbnailUrl = topic.ThumbnailUrl,
                        IsAIGenerated = true,
                        ModerationReason = generated.ReviewNotes
                    };

                    var pollId = await _pollsRepo.CreateAsync(request);
                    await _topicRepo.MarkProcessedAsync(topic.Id);

                    _logger.LogInformation(
                        "[PollGenerationJob] Created generated poll {PollId} from topic {TopicId}. SourceUrl={SourceUrl}. SimilarPollId={SimilarPollId}. ReviewNotes={ReviewNotes}",
                        pollId,
                        topic.Id,
                        topic.SourceUrl,
                        generated.SimilarPollId,
                        generated.ReviewNotes);

                    created++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[PollGenerationJob] Failed to save generated poll for topic {TopicId} '{Title}'. Question={Question}. SourceUrl={SourceUrl}",
                        topic.Id,
                        topic.Title,
                        generated.Proposition,
                        topic.SourceUrl);
                }
            }

            _logger.LogInformation(
                "[PollGenerationJob] Done. Created: {Created}, skipped: {Skipped}",
                created,
                skipped);
        }
    }
}
