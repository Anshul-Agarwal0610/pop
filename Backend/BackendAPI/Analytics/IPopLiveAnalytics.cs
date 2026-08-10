namespace BackendAPI.Analytics;
public interface IPopLiveAnalytics
{
    Task TrackAsync(int actorId, string eventName, string semanticKey, PopLiveEventProperties properties, CancellationToken cancellationToken = default);
}
