using BackendAPI.Models;

namespace BackendAPI.Interfaces;

public interface IMultiplayerTrustRepository
{
    Task<SafetyReportReceipt> CreateReportAsync(long? reporterUserId, Guid? reporterParticipantId, CreateSafetyReportRequest request);
    Task LeaveAsync(Guid sessionId, long? userId, string? reconnectToken);
    Task<MultiplayerPrivacySettings> GetPrivacyAsync(long userId);
    Task SavePrivacyAsync(long userId, MultiplayerPrivacySettings settings);
    Task<MultiplayerNotificationSettings> GetNotificationsAsync(long userId);
    Task SaveNotificationsAsync(long userId, MultiplayerNotificationSettings settings);
}

public interface IMultiplayerRewardRiskEvaluator
{
    MultiplayerRiskDecision Evaluate(MultiplayerRiskContext context, DateTime? evaluatedAt = null);
}
