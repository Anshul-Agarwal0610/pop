using BackendAPI.Models;

namespace BackendAPI.Interfaces
{
    public interface IPollGenerationService
    {
        /// <summary>
        /// Calls the Llama/Mistral VM endpoint to generate a poll from a trending topic.
        /// Returns null when the model cannot produce a valid poll.
        /// </summary>
        Task<GeneratedPoll?> GenerateAsync(TrendingTopic topic);
    }

    public class GeneratedPoll
    {
        public string Question { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public string Category { get; set; } = "General";
    }
}
