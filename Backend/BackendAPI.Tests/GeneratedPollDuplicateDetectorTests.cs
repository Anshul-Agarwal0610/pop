using BackendAPI.Interfaces;
using BackendAPI.Models;
using BackendAPI.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace BackendAPI.Tests;

public class GeneratedPollDuplicateDetectorTests
{
    [Fact]
    public void Fingerprint_normalizes_unicode_case_punctuation_and_stop_words()
    {
        var detector = Create([]);
        Assert.Equal(detector.Fingerprint("Should THE Café adopt a policy?"), detector.Fingerprint("cafe adopt policy"));
    }

    [Fact]
    public async Task Same_canonical_source_url_is_exact_match()
    {
        var detector = Create([new Poll { Id = 7, Question = "An unrelated generated question here?", SourceUrl = "https://EXAMPLE.test/story/?tracking=x" }]);
        var match = await detector.FindAsync("Should a different policy now be adopted?", "https://example.test/story/");
        Assert.NotNull(match);
        Assert.Equal("source_url", match!.MatchType);
    }

    [Fact]
    public async Task Near_duplicate_is_routed_with_similarity()
    {
        var detector = Create([new Poll { Id = 8, Question = "Should Parliament adopt the proposed national data privacy law?" }], .6);
        var match = await detector.FindAsync("Should Parliament adopt a proposed data privacy law?", null);
        Assert.NotNull(match);
        Assert.Equal("near", match!.MatchType);
        Assert.InRange(match.Similarity, .6, .999);
    }

    private static GeneratedPollDuplicateDetector Create(IEnumerable<Poll> polls, double threshold = .78) =>
        new(new FakeRepository(polls), Options.Create(new PollQualityOptions { DuplicateSimilarityThreshold = threshold }));

    private sealed class FakeRepository(IEnumerable<Poll> polls) : IPollsRepository
    {
        public Task<IEnumerable<Poll>> GetRecentGeneratedAsync(int count = 100) => Task.FromResult(polls);
        public Task<long> CreateAsync(CreatePollRequest request, long? id = null) => throw new NotImplementedException();
        public Task<bool> DeleteAsync(long id) => throw new NotImplementedException();
        public Task<IEnumerable<Poll>> GetAllAsync(long? userId = null, string? category = null) => throw new NotImplementedException();
        public Task<Poll?> GetByIdAsync(long id, long? userId = null) => throw new NotImplementedException();
        public Task<IEnumerable<Poll>> GetModerationQueueAsync(string? status = null, int count = 50) => throw new NotImplementedException();
        public Task<IEnumerable<Poll>> GetPersonalizedAsync(long? userId = null, int count = 20, string? category = null) => throw new NotImplementedException();
        public Task<IEnumerable<Poll>> GetRecentAsync(int count = 10) => throw new NotImplementedException();
        public Task<IEnumerable<Poll>> GetTrendingAsync(int count = 10, long? userId = null, string? category = null) => throw new NotImplementedException();
        public Task<bool> ModerateAsync(long pollId, string status, string? reason, long id) => throw new NotImplementedException();
        public Task<bool> ReportAsync(long pollId, long id, string reason) => throw new NotImplementedException();
        public Task<IEnumerable<Poll>> SearchAsync(string query, string? category = null, long? userId = null) => throw new NotImplementedException();
        public Task UpdateTrendingAsync() => throw new NotImplementedException();
    }
}
