using ChatApp.Web.Data;

namespace ChatApp.Web.Services;

public class MessageService : IMessageService
{
    private readonly AppDbContext _db;

    public MessageService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ChatMessage> SaveRoomMessageAsync(int roomId, Guid senderUserId, string content)
    {
        var message = new ChatMessage
        {
            ChatRoomId = roomId,
            SenderUserId = senderUserId,
            Content = content
        };

        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();

        return message;
    }

    public async Task<ChatMessage> SavePrivateMessageAsync(Guid senderUserId, Guid recipientUserId, string content)
    {
        var message = new ChatMessage
        {
            RecipientUserId = recipientUserId,
            SenderUserId = senderUserId,
            Content = content
        };

        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();

        return message;
    }
}