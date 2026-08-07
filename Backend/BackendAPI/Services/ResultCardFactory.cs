using BackendAPI.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackendAPI.Services;

public sealed class ResultCardFactory
{
    public const int SchemaVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public ResultCardPayload Create(NormalizedMultiplayerResult result)
    {
        if (!MultiplayerModes.IsSupported(result.Mode))
            throw new ResultCardException("unsupported_mode", "Result cards support Clash, Relay and Room.");
        if (result.State is not (ResultCardState.Completed or ResultCardState.Active))
            throw new ResultCardException("invalid_state", "Only active invitations or completed results can be shared.");
        if (string.IsNullOrWhiteSpace(result.AggregateResult) || result.Participants.Count == 0)
            throw new ResultCardException("invalid_result", "The server result is incomplete.");

        var participants = result.Participants.Select((participant, index) =>
            participant.PublicCardConsent
                ? new ResultCardParticipant(Clean(participant.DisplayName, 60), SafeAvatar(participant.AvatarUrl), false)
                : new ResultCardParticipant($"Participant {index + 1}"))
            .ToArray();
        var aggregate = Clean(result.AggregateResult, 140);
        var milestone = string.IsNullOrWhiteSpace(result.Milestone) ? null : Clean(result.Milestone, 80);
        var badge = result.EarnedBadge is null ? null : new ResultCardBadge(Clean(result.EarnedBadge.Name, 60), Clean(result.EarnedBadge.Icon, 24));
        var summary = $"{result.Mode} with {participants.Length} participants: {aggregate}"
            + (milestone is null ? string.Empty : $". {milestone}")
            + (badge is null ? string.Empty : $". Badge earned: {badge.Name}");

        return new ResultCardPayload(SchemaVersion, result.Mode, result.State, aggregate, milestone, badge,
            participants.Length, participants, summary);
    }

    public string Serialize(ResultCardPayload payload) => JsonSerializer.Serialize(payload, JsonOptions);
    public ResultCardPayload Deserialize(string json) => JsonSerializer.Deserialize<ResultCardPayload>(json, JsonOptions)
        ?? throw new InvalidOperationException("Stored result card payload is invalid.");
    public string Hash(ResultCardPayload payload) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Serialize(payload)))).ToLowerInvariant();

    private static string Clean(string value, int max) => value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static string? SafeAvatar(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps ? uri.ToString() : null;
}
