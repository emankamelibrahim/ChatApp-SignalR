using ChatApp.Web.Data;

namespace ChatApp.Web.Services;

public interface IRoomService
{
    Task<ChatRoom> CreateRoomAsync(string name, Guid createdByUserId, List<Guid> memberUserIds);
    Task<bool> DeleteRoomAsync(int roomId, Guid requestingUserId);
    Task<List<ChatRoom>> GetRoomsForUserAsync(Guid userId);
    Task<ChatRoom?> GetRoomByIdAsync(int roomId);
    Task<bool> IsUserMemberOfRoomAsync(int roomId, Guid userId);
}