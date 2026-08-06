namespace BackendAPI.Services;

public static class GameSessionRules
{
    public static bool IsExpired(DateTime? expiresAt, DateTime utcNow) => expiresAt is not null && utcNow >= expiresAt.Value;
    public static bool IsCurrentPosition(int currentPosition, int submittedPosition) => currentPosition == submittedPosition;
    public static bool CanGrantCompletionReward(string status, DateTime? rewardGrantedAt, int nextPosition, int pollCount) =>
        status == "Active" && rewardGrantedAt is null && nextPosition == pollCount;
}
