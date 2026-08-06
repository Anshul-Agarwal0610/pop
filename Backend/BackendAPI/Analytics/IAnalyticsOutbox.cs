using System.Data;
namespace BackendAPI.Analytics;
public interface IAnalyticsOutbox { Task EnqueueAsync(IDbConnection connection, IDbTransaction transaction, AnalyticsEvent analyticsEvent); }
