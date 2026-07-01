namespace ChatApp.Web.Data;

public class ChatRoomMember
{
    public int Id { get; set; }

    public int ChatRoomId { get; set; }
    public ChatRoom ChatRoom { get; set; } = null!;

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}