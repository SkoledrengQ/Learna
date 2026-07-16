using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(int id);
    Task<Conversation?> FindDirectAsync(int userAId, int userBId);
    Task<Conversation> AddAsync(Conversation conversation);
    Task<IReadOnlyList<Conversation>> GetActiveForUserAsync(int userId);
    Task<IReadOnlyList<Conversation>> SearchAllAsync(string? search);
    Task<IReadOnlyDictionary<int, Message>> GetLastMessagesAsync(IEnumerable<int> conversationIds);
    Task<IReadOnlyDictionary<int, int>> GetUnreadCountsAsync(int userId);
    Task<int> GetMessageCountAsync(int conversationId);
    Task<ConversationParticipant?> GetParticipantAsync(int conversationId, int userId);
    Task<IReadOnlyList<ConversationParticipant>> GetActiveParticipantsAsync(int conversationId);
    Task<ConversationParticipant> AddParticipantAsync(int conversationId, int userId, DateTime now);
    Task RemoveParticipantAsync(ConversationParticipant participant, DateTime now);
    Task<IReadOnlyList<Message>> GetMessagesAsync(int conversationId, int? beforeId, int take);
    Task<Message> AddMessageAsync(Message message);
    Task MarkReadAsync(int conversationId, int userId, DateTime readAt);
    Task SaveAsync();
}
