using ChatApp.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using ChatApp.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Web.Hubs;

[Authorize(AuthenticationSchemes = "Identity.Application,Bearer")]
public class ChatHub : Hub
{
    private readonly IRoomService _roomService;
    private readonly IMessageService _messageService;
    private readonly AppDbContext _db;

    public ChatHub(IRoomService roomService, IMessageService messageService, AppDbContext db)
    {
        _roomService = roomService;
        _messageService = messageService;
        _db = db;
    }

    private Guid CurrentUserId => Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentUserName => Context.User!.Identity!.Name!;

    private static string RoomGroupName(int roomId) => $"room-{roomId}";

    public override async Task OnConnectedAsync()
    {
        var rooms = await _roomService.GetRoomsForUserAsync(CurrentUserId);

        foreach (var room in rooms)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroupName(room.Id));
        }

        await Clients.All.SendAsync("ReceiveActivity", $"{CurrentUserName} connected.");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Clients.All.SendAsync("ReceiveActivity", $"{CurrentUserName} disconnected.");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task CreateRoom(string roomName, List<Guid> memberUserIds)
    {
        var room = await _roomService.CreateRoomAsync(roomName, CurrentUserId, memberUserIds);

        await NotifyRoomCreated(room.Id, room.Name, room.Members.Select(m => m.UserId).ToList());
    }

    private async Task NotifyRoomCreated(int roomId, string roomName, List<Guid> memberUserIds)
    {
        var allMemberIdStrings = memberUserIds.Select(id => id.ToString()).ToList();

        await Clients.Users(allMemberIdStrings).SendAsync("RoomCreated", roomId, roomName, CurrentUserId);
        await Clients.Users(allMemberIdStrings).SendAsync("ReceiveActivity", $"{CurrentUserName} created {roomName}.");
    }

    public async Task JoinRoomGroup(int roomId)
    {
        var isMember = await _roomService.IsUserMemberOfRoomAsync(roomId, CurrentUserId);

        if (isMember)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroupName(roomId));
        }
    }

    public async Task DeleteRoom(int roomId)
    {
        var roomToDelete = await _roomService.GetRoomByIdAsync(roomId);

        if (roomToDelete is null)
        {
            return; 
        }

        var roomName = roomToDelete.Name;
        var memberIds = roomToDelete.Members.Select(m => m.UserId).ToList();

        var success = await _roomService.DeleteRoomAsync(roomId, CurrentUserId);

        if (!success)
        {
            await Clients.Caller.SendAsync("ReceiveActivity", "Only the room creator can delete this room.");
            return;
        }

        await Clients.Group(RoomGroupName(roomId)).SendAsync("ReceiveActivity", $"{CurrentUserName} deleted {roomName}.");
        await Clients.Users(memberIds.Select(id => id.ToString())).SendAsync("RoomDeleted", roomId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomGroupName(roomId));
    }

    public async Task SendRoomMessage(int roomId, string content)
    {
        var isMember = await _roomService.IsUserMemberOfRoomAsync(roomId, CurrentUserId);

        if (!isMember)
        {
            await Clients.Caller.SendAsync("ReceiveActivity", "You are not a member of this room.");
            return;
        }

        await _messageService.SaveRoomMessageAsync(roomId, CurrentUserId, content);

        await Clients.Group(RoomGroupName(roomId))
            .SendAsync("ReceiveRoomMessage", roomId, CurrentUserName, content);
    }

    public async Task SendPrivateMessage(Guid recipientUserId, string content)
    {
        await _messageService.SavePrivateMessageAsync(CurrentUserId, recipientUserId, content);

        await Clients.User(recipientUserId.ToString())
            .SendAsync("ReceivePrivateMessage", CurrentUserName, content);

        await Clients.Caller
            .SendAsync("ReceivePrivateMessage", CurrentUserName, content);
    }

    public async Task<List<RoomDto>> GetMyRooms()
    {
        var rooms = await _roomService.GetRoomsForUserAsync(CurrentUserId);
        return rooms.Select(r => new RoomDto(r.Id, r.Name, r.CreatedByUserId)).ToList();
    }

    public async Task<List<UserDto>> GetAllUsers()
    {
        var users = await _db.Users
            .Where(u => u.Id != CurrentUserId)
            .Select(u => new UserDto(u.Id, u.Email!))
            .ToListAsync();
        return users;
    }

    public record RoomDto(int Id, string Name, Guid CreatedByUserId);
    public record UserDto(Guid Id, string Email);
}