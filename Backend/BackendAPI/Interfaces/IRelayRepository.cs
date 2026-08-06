using BackendAPI.Models;

namespace BackendAPI.Interfaces;

public interface IRelayRepository
{
    Task<RelayStartResult> StartAsync(long userId, StartRelayRequest request, DateTime utcNow);
    Task<RelayHandoffView?> GetHandoffAsync(string token, long? userId, DateTime utcNow);
    Task AcceptAsync(string token, long userId, DateTime utcNow);
    Task<RelayCompleteResult> CompleteAsync(string token, long userId, CompleteRelayRequest request, DateTime utcNow);
    Task<RelayProgress?> GetProgressAsync(long chainId, long userId, DateTime utcNow);
    Task SetConsentAsync(long chainId, long userId, bool receive);
    Task<RelayOutcome?> GetOutcomeAsync(long chainId, long userId);
    Task<int> ExpireOverdueAsync(DateTime utcNow);
}
