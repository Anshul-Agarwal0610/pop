namespace BackendAPI.Analytics;
public sealed class AnalyticsOptions { public const string Section = "Analytics"; public bool Enabled { get; set; } = false; public string CaptureUrl { get; set; } = ""; public string ApiKey { get; set; } = ""; public string AppVersion { get; set; } = "unknown"; }
