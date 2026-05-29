namespace BackendAPI.Interfaces
{
    public interface IPushNotificationService
    {
        Task<int> SendPendingAsync(int count = 100);
    }
}
