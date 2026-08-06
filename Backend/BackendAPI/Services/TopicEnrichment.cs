using BackendAPI.Interfaces;
using BackendAPI.Models;
using System.Net;
using System.Text.RegularExpressions;

namespace BackendAPI.Services
{
    public static class TopicEnrichment
    {
        private static readonly (string Category, string[] Keywords)[] CategoryRules =
        {
            ("Technology", new[] { " ai ", "artificial intelligence", "google", "microsoft", "apple", "software", "app ", "cyber", "smartphone", "tech", "nasa", "space" }),
            ("Sports", new[] { "cricket", "football", "hockey", "tennis", "match", "tournament", "player", "coach", "bcci", "fifa", "ipl" }),
            ("Health", new[] { "health", "hospital", "doctor", "disease", "medical", "vaccine", "virus", "cancer", "fitness", "screening" }),
            ("Work", new[] { "market", "business", "company", "economy", "finance", "stock", "ipo", "bank", "investment", "tax", "jobs" }),
            ("Environment", new[] { "climate", "pollution", "environment", "weather", "rain", "flood", "wildfire", "energy" }),
            ("Culture", new[] { "film", "movie", "music", "actor", "actress", "celebrity", "box office", "festival", "streaming" }),
            ("Politics", new[] { "government", "minister", "parliament", "election", "president", "prime minister", "policy", "court", "party", "congress", "bjp" }),
            ("Society", new[] { "education", "student", "school", "university", "community", "rights", "crime", "police", "housing" }),
        };

        private static readonly string[] SensitiveKeywords =
        {
            "suicide", "self-harm", "rape", "sexual assault", "murder", "killed", "death",
            "dead", "stabbing", "shooting", "gunman", "child abuse", "domestic violence"
        };

        public static string CleanText(string? value)
        {
            var decoded = WebUtility.HtmlDecode(value ?? string.Empty);
            return Regex.Replace(decoded, @"\s+", " ").Trim();
        }

        public static string Classify(string? title, string? summary, string? sourceCategory = null)
        {
            var normalizedHint = CategoryCatalog.NormalizeName(sourceCategory);
            if (!normalizedHint.Equals(CategoryCatalog.DefaultCategoryName, StringComparison.OrdinalIgnoreCase))
                return normalizedHint;

            var searchable = $" {CleanText(title)} {CleanText(summary)} ".ToLowerInvariant();
            foreach (var (category, keywords) in CategoryRules)
            {
                if (keywords.Any(searchable.Contains)) return category;
            }

            return CategoryCatalog.DefaultCategoryName;
        }

        public static bool RequiresHumanReview(TrendingTopic topic)
        {
            var searchable = $"{CleanText(topic.Title)} {CleanText(topic.Summary)}".ToLowerInvariant();
            return SensitiveKeywords.Any(searchable.Contains);
        }

        public static GeneratedPoll CreateFallbackPoll(TrendingTopic topic)
        {
            const string prefix = "What should matter most in this story: ";
            var title = CleanText(topic.Title).TrimEnd('.', '?', '!', ':', ';');
            var available = 119 - prefix.Length;
            if (title.Length > available)
                title = title[..Math.Max(1, available - 1)].TrimEnd() + "…";

            var category = Classify(title, topic.Summary, topic.Category);
            List<string> options = category switch
            {
                "Technology" => new() { "User benefits", "Privacy and safety", "Cost and access", "Long-term impact" },
                "Sports" => new() { "Player performance", "Team strategy", "Coaching decisions", "Future prospects" },
                "Health" => new() { "Prevention", "Access to care", "Public awareness", "Research and policy" },
                "Work" => new() { "Consumer impact", "Market response", "Jobs and livelihoods", "Long-term growth" },
                "Environment" => new() { "Immediate protection", "Policy response", "Public awareness", "Long-term resilience" },
                "Culture" => new() { "Creative quality", "Audience response", "Social impact", "Lasting influence" },
                _ => new() { "Public impact", "Official response", "Long-term effects", "More information" },
            };

            var poll = new GeneratedPoll
            {
                Question = $"{prefix}{title}?",
                Options = options,
                Category = category,
                SourceTitle = topic.Title,
                SourceUrl = topic.SourceUrl,
            };
            poll.QualityWarnings.Add("Generated with the deterministic fallback because the LLM was unavailable.");
            return poll;
        }
    }
}
