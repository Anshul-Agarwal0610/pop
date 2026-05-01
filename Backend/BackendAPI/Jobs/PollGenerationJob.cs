using BackendAPI.Interfaces;
using BackendAPI.Models;

namespace BackendAPI.Jobs
{
    /// <summary>
    /// US-08 — Hangfire recurring job that reads unprocessed TrendingTopics,
    /// calls the poll-generation VM for each, persists the resulting poll,
    /// and marks the topic as processed.
    ///
    /// Scheduled: every 35 minutes offset by 5 min (cron: "5/35 * * * *")
    /// so ingestion always runs first.
    /// </summary>
    public class PollGenerationJob
    {
        private readonly ITrendingTopicRepository _topicRepo;
        private readonly IPollsRepository         _pollsRepo;
        private readonly IPollGenerationService   _generator;
        private readonly ILogger<PollGenerationJob> _logger;

        // Polls expire 48 hours after creation by default
        private static readonly TimeSpan DefaultExpiry = TimeSpan.FromHours(48);

        public PollGenerationJob(
            ITrendingTopicRepository topicRepo,
            IPollsRepository pollsRepo,
            IPollGenerationService generator,
            ILogger<PollGenerationJob> logger)
        {
            _topicRepo = topicRepo;
            _pollsRepo = pollsRepo;
            _generator = generator;
            _logger    = logger;
        }

        public async Task RunAsync()
        {
            _logger.LogInformation("[PollGenerationJob] Starting at {Time}", DateTime.UtcNow);

            var topics = (await _topicRepo.GetUnprocessedAsync(maxCount: 20)).ToList();

            if (topics.Count == 0)
            {
                _logger.LogInformation("[PollGenerationJob] No unprocessed topics — nothing to do");
                return;
            }

            _logger.LogInformation("[PollGenerationJob] Processing {Count} topics", topics.Count);

            int created = 0, skipped = 0;

            foreach (var topic in topics)
            {
                var generated = await _generator.GenerateAsync(topic);

                if (generated == null)
                {
                    _logger.LogWarning("[PollGenerationJob] Skipping topic {Id} — generation returned null", topic.Id);
                    skipped++;
                    // Still mark as processed so we don't retry indefinitely
                    await _topicRepo.MarkProcessedAsync(topic.Id);
                    continue;
                }

                try
                {
                    var request = new CreatePollRequest
                    {
                        Question     = generated.Question,
                        Description  = topic.Summary,
                        Category     = generated.Category,
                        ExpiresAt    = DateTime.UtcNow.Add(DefaultExpiry),
                        Options      = generated.Options,
                        SourceType   = topic.SourceType,
                        SourceUrl    = topic.SourceUrl,
                        ThumbnailUrl = topic.ThumbnailUrl,
                        IsAIGenerated = true
                    };

                    var pollId = await _pollsRepo.CreateAsync(request);
                    await _topicRepo.MarkProcessedAsync(topic.Id);

                    _logger.LogInformation(
                        "[PollGenerationJob] Created poll {PollId} from topic {TopicId}: '{Question}'",
                        pollId, topic.Id, generated.Question);

                    created++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[PollGenerationJob] Failed to save poll for topic {TopicId}", topic.Id);
                }
            }

            _logger.LogInformation(
                "[PollGenerationJob] Done — created: {Created}, skipped: {Skipped}",
                created, skipped);
        }
    }
}
