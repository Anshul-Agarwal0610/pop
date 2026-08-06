using BackendAPI.Models;
namespace BackendAPI.Services;
public static class LiveRoomRules
{
    public static void EnsureTransition(LiveRoomStatus room, string command)
    {
        var valid = command switch {
            "start" => room == LiveRoomStatus.Lobby, "pause" => room == LiveRoomStatus.Active,
            "resume" => room == LiveRoomStatus.Paused, "advance" => room is LiveRoomStatus.Active or LiveRoomStatus.Paused,
            "close" => room == LiveRoomStatus.Active,
            "end" => room is not (LiveRoomStatus.Ended or LiveRoomStatus.Expired), _ => false };
        if (!valid) throw new LiveRoomException("invalid_transition", $"Cannot {command} a {room} room.");
    }
    public static bool IsEligible(int eligibleFrom, int position) => eligibleFrom <= position;
    public static void EnsureCanJoin(int active, int limit, LiveRoomStatus status, DateTime expiresAt, DateTime now)
    { if (now >= expiresAt || status == LiveRoomStatus.Expired) throw new LiveRoomException("expired", "Room expired.");
      if (status == LiveRoomStatus.Ended) throw new LiveRoomException("ended", "Room ended.");
      if (active >= limit) throw new LiveRoomException("capacity", "Room is full."); }
}
