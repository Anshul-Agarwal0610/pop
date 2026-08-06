namespace BackendAPI.Models;

public enum LiveRoomStatus { Lobby, Active, Paused, Ended, Expired }
public enum LiveRoundStatus { Pending, Open, Paused, Closed, Revealed }
public enum LiveRoomMode { PredictMajority, ConsensusChallenge }
public enum BinaryChoice { Up, Against }

public sealed record LiveRoomRuleConfig(double ConsensusThreshold = .75, int CorrectPredictionPoints = 1,
    int ConsensusPoints = 1, string Strategy = "Default");
public sealed record CreateLiveRoomRequest(long PackId, LiveRoomMode Mode, int ParticipantLimit = 50,
    LiveRoomRuleConfig? Rules = null);
public sealed record JoinLiveRoomRequest(string Code, string DisplayName, string? ReconnectToken = null);
public sealed record LiveVoteRequest(BinaryChoice Choice, BinaryChoice? PredictedMajority = null);
public sealed record ParticipantDto(Guid Id, string DisplayName, int Score, bool Connected, bool Eligible);
public sealed record LiveRoundDto(int Position, string Proposition, LiveRoundStatus Status, int Submitted,
    int Eligible, int? Up = null, int? Against = null);
public sealed record HostRoomSnapshot(Guid Id, string Code, LiveRoomStatus Status, LiveRoomMode Mode, long Version,
    IReadOnlyList<ParticipantDto> Participants, LiveRoundDto? Round, string DisplayToken);
public sealed record ParticipantRoomSnapshot(Guid RoomId, Guid ParticipantId, LiveRoomStatus Status,
    LiveRoomMode Mode, long Version, int Score, bool Eligible, bool HasVoted, LiveRoundDto? Round);
// Deliberately contains no host identity, controls, credentials or individual votes.
public sealed record DisplayRoomSnapshot(Guid RoomId, string Code, LiveRoomStatus Status, LiveRoomMode Mode,
    long Version, int ParticipantCount, IReadOnlyList<ParticipantDto> Scoreboard, LiveRoundDto? Round);
public sealed record JoinLiveRoomResponse(Guid RoomId, Guid ParticipantId, string ReconnectToken,
    ParticipantRoomSnapshot Snapshot);
public sealed class LiveRoomException(string code, string message) : Exception(message) { public string Code { get; } = code; }

internal sealed class LiveRoomState
{
    public Guid Id { get; init; } = Guid.NewGuid(); public long HostId { get; init; }
    public string Code { get; init; } = ""; public string DisplayToken { get; init; } = "";
    public LiveRoomMode Mode { get; init; } public LiveRoomRuleConfig Rules { get; init; } = new();
    public int Limit { get; init; } public LiveRoomStatus Status { get; set; }
    public long Version { get; set; } = 1; public DateTime ExpiresAt { get; set; }
    public int Position { get; set; } = -1; public List<string> Propositions { get; init; } = [];
    public List<LiveParticipantState> Participants { get; init; } = []; public LiveRoundState? Round { get; set; }
}
internal sealed class LiveParticipantState
{
    public Guid Id { get; init; } = Guid.NewGuid(); public string Name { get; init; } = "";
    public string TokenHash { get; init; } = ""; public int Score { get; set; }
    public bool Removed { get; set; } public int EligibleFrom { get; init; }
}
internal sealed class LiveRoundState
{
    public int Position { get; init; } public string Proposition { get; init; } = "";
    public LiveRoundStatus Status { get; set; } = LiveRoundStatus.Open;
    public Dictionary<Guid, (BinaryChoice Choice, BinaryChoice? Prediction)> Votes { get; } = [];
}
