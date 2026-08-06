namespace BackendAPI.Analytics;
public interface IFeatureFlagService { bool IsEnabled(string flag, string subject, int rolloutPercent = 100); string Variant(string flag, string subject, int rolloutPercent = 50); }
public sealed class FeatureFlagService : IFeatureFlagService
{
    public bool IsEnabled(string flag, string subject, int rolloutPercent = 100) => Bucket(flag, subject) < Math.Clamp(rolloutPercent, 0, 100) * 100;
    public string Variant(string flag, string subject, int rolloutPercent = 50) => IsEnabled(flag, subject, rolloutPercent) ? "treatment" : "control";
    public static int Bucket(string flag, string subject) { unchecked { uint hash=2166136261; foreach (var c in $"{flag}:{subject}") { hash ^= c; hash *= 16777619; } return (int)(hash % 10000); } }
}
