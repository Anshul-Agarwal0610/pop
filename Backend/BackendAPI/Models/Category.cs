namespace BackendAPI.Models
{
    public sealed record PollCategory(
        int Id,
        string Name,
        string Slug,
        string Icon,
        string Color,
        int SortOrder,
        bool IsActive = true);

    public static class CategoryCatalog
    {
        public const string DefaultCategoryName = "General";

        public static readonly IReadOnlyList<PollCategory> All = new List<PollCategory>
        {
            new(1, DefaultCategoryName, "general", "sparkles", "slate", 10),
            new(2, "Technology", "technology", "cpu", "blue", 20),
            new(3, "Society", "society", "users", "rose", 30),
            new(4, "Work", "work", "briefcase", "amber", 40),
            new(5, "Environment", "environment", "leaf", "emerald", 50),
            new(6, "Culture", "culture", "palette", "violet", 60),
            new(7, "Sports", "sports", "trophy", "orange", 70),
            new(8, "Health", "health", "heart-pulse", "teal", 80),
            new(9, "Politics", "politics", "landmark", "indigo", 90),
        };

        private static readonly Dictionary<string, PollCategory> ByName =
            All.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, PollCategory> BySlug =
            All.ToDictionary(c => c.Slug, StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> Aliases =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["tech"] = "Technology",
                ["business"] = "Work",
                ["career"] = "Work",
                ["jobs"] = "Work",
                ["climate"] = "Environment",
                ["entertainment"] = "Culture",
                ["arts"] = "Culture",
                ["movies"] = "Culture",
                ["wellness"] = "Health",
                ["medical"] = "Health",
                ["fitness"] = "Health",
                ["news"] = "Politics",
                ["government"] = "Politics",
            };

        public static PollCategory Normalize(string? category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return ByName[DefaultCategoryName];
            }

            var key = category.Trim();
            if (ByName.TryGetValue(key, out var byName))
            {
                return byName;
            }

            var slug = Slugify(key);
            if (BySlug.TryGetValue(slug, out var bySlug))
            {
                return bySlug;
            }

            if (Aliases.TryGetValue(key, out var aliasName) || Aliases.TryGetValue(slug, out aliasName))
            {
                return ByName[aliasName];
            }

            return ByName[DefaultCategoryName];
        }

        public static string NormalizeName(string? category) => Normalize(category).Name;

        private static string Slugify(string value)
        {
            return value.Trim().ToLowerInvariant().Replace(" ", "-").Replace("_", "-");
        }
    }
}
