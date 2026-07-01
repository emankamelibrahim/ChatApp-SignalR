using ChatApp.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Web.Services;

public class RoomService : IRoomService
{
    private readonly AppDbContext _db;

    public RoomService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ChatRoom> CreateRoomAsync(string name, Guid createdByUserId, List<Guid> memberUserIds)
    {
        var room = new ChatRoom
        {
            Name = name,
            CreatedByUserId = createdByUserId
        };

        var allMemberIds = memberUserIds
            .Append(createdByUserId)
            .Distinct()
            .ToList();

        foreach (var userId in allMemberIds)
        {
            room.Members.Add(new ChatRoomMember
            {
                UserId = userId
            });
        }

        _db.ChatRooms.Add(room);
        await _db.SaveChangesAsync();

        return room;
    }

    public async Task<ChatRoom?> GetRoomByIdAsync(int roomId)
    {
        return await _db.ChatRooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Id == roomId);
    }
    public async Task<bool> DeleteRoomAsync(int roomId, Guid requestingUserId)
    {
        var room = await _db.ChatRooms.FirstOrDefaultAsync(r => r.Id == roomId);

        if (room is null || room.CreatedByUserId != requestingUserId)
        {

            return false;
        }

        _db.ChatRooms.Remove(room);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<List<ChatRoom>> GetRoomsForUserAsync(Guid userId)
    {
        return await _db.ChatRooms
            .Where(r => r.Members.Any(m => m.UserId == userId))
            .ToListAsync();
    }

    public async Task<bool> IsUserMemberOfRoomAsync(int roomId, Guid userId)
    {
        return await _db.ChatRoomMembers
            .AnyAsync(m => m.ChatRoomId == roomId && m.UserId == userId);
    }
}