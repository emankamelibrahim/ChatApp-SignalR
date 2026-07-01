namespace ChatApp.Web.Data;

public class ChatMessage
{
    public int Id { get; set; }

    public Guid SenderUserId { get; set; }
    public ApplicationUser SenderUser { get; set; } = null!;

    public int? ChatRoomId { get; set; }
    public ChatRoom? ChatRoom { get; set; }

    public Guid? RecipientUserId { get; set; }
    public ApplicationUser? RecipientUser { get; set; }

    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}