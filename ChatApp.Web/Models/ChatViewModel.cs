using ChatApp.Web.Data;

namespace ChatApp.Web.Models;

public class ChatViewModel
{
    public Guid CurrentUserId { get; set; }
    public string CurrentUserName { get; set; } = string.Empty;
    public List<ChatRoom> Rooms { get; set; } = [];
    public List<UserListItem> OtherUsers { get; set; } = [];
}

public class UserListItem
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
}