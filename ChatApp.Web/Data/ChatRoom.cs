namespace ChatApp.Web.Data;

public class ChatRoom
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser CreatedByUser { get; set; } = null!;
    public List<ChatRoomMember> Members { get; set; } = [];
    public List<ChatMessage> Messages { get; set; } = [];
}