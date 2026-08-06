using BackendAPI.Interfaces; using BackendAPI.Services;
namespace BackendAPI.Jobs;
public sealed class LiveRoomExpirationJob(ILiveRoomsRepository rooms,ISystemClock clock){public Task RunAsync()=>rooms.ExpireAsync(clock.UtcNow);}
