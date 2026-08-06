namespace BackendAPI.Models;

public static class PollClashStatuses { public const string Lobby="Lobby", Active="Active", Completed="Completed", Expired="Expired"; }
public static class PollClashSources { public const string Poll="Poll", GeneratedPack="GeneratedPack"; }
public static class PollClashRoundStatuses { public const string Pending="Pending", Active="Active", Revealed="Revealed"; }

public sealed record CreatePollClashRequest(long? SeedPollId, string Source, int RoundCount);
public sealed record PollClashResponseRequest(long RoundId, long OpinionOptionId, long? PredictedMajorityOptionId);
public sealed record PollClashOptionDto(long Id, string Text, int? PublicVotes);
public sealed record PollClashRevealedOpinionDto(long UserId, string DisplayName, long OpinionOptionId, long? PredictedMajorityOptionId, int PredictionPoint);
public sealed class PollClashPlayerDto
{
 public PollClashPlayerDto(long userId,string displayName,bool isViewer,bool hasSubmitted,long? opinionOptionId,long? predictedMajorityOptionId,int predictionScore){UserId=userId;DisplayName=displayName;IsViewer=isViewer;HasSubmitted=hasSubmitted;OpinionOptionId=isViewer?opinionOptionId:null;PredictedMajorityOptionId=isViewer?predictedMajorityOptionId:null;PredictionScore=predictionScore;}
 public long UserId{get;} public string DisplayName{get;} public bool IsViewer{get;} public bool HasSubmitted{get;} public long? OpinionOptionId{get;} public long? PredictedMajorityOptionId{get;} public int PredictionScore{get;}
}
public sealed record PollClashRoundDto(long Id, int Position, long PollId, string Question, string Status, IReadOnlyList<PollClashOptionDto> Options, long? ResolvedMajorityOptionId, bool? Agreed, int PredictionPointsAwarded, IReadOnlyList<PollClashRevealedOpinionDto> RevealedOpinions);
public sealed record PollClashRewardDto(int AwardedXp, bool IsDuplicate, bool CapReached);
public sealed record PollClashRematchDto(long Id, long RequestedByUserId, string Status, long? ResultingClashId);
public sealed record PollClashDto(long Id, string InviteCode, string Status, string Source, int RoundCount, int CompletedRounds, DateTime ExpiresAt, IReadOnlyList<PollClashPlayerDto> Players, IReadOnlyList<PollClashRoundDto> Rounds, int AgreementCount, long? WinnerUserId, PollClashRewardDto Reward, PollClashRematchDto? Rematch);

public sealed class PollClashException(string code, string message) : InvalidOperationException(message) { public string Code { get; } = code; }

public sealed record PollClashScore(int FirstPredictionScore, int SecondPredictionScore, int AgreementCount, int CompletedRounds)
{
    public int? WinnerIndex => FirstPredictionScore == SecondPredictionScore ? null : FirstPredictionScore > SecondPredictionScore ? 0 : 1;
}
