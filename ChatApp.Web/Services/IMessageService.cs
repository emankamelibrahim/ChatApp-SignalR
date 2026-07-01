using ChatApp.Web.Data;

namespace ChatApp.Web.Services;

public interface IMessageService
{
    Task<ChatMessage> SaveRoomMessageAsync(int roomId, Guid senderUserId, string content);
    Task<ChatMessage> SavePrivateMessageAsync(Guid senderUserId, Guid recipientUserId, string content);
}