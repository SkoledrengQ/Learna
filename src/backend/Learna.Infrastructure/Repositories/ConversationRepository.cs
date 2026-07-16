using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learna.Infrastructure.Repositories;

public sealed class ConversationRepository(ApplicationDbContext context) : IConversationRepository
{
    private IQueryable<Conversation> ConversationsWithParticipants => context.Conversations
        .Include(c => c.CreatedByUser)
        .Include(c => c.Participants).ThenInclude(p => p.User).ThenInclude(u => u.Student)
        .Include(c => c.Participants).ThenInclude(p => p.User).ThenInclude(u => u.Teacher)
        .Include(c => c.Participants).ThenInclude(p => p.User).ThenInclude(u => u.Guardian)
        .Include(c => c.Participants).ThenInclude(p => p.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role);

    public Task<Conversation?> GetByIdAsync(int id) => ConversationsWithParticipants.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Conversation?> FindDirectAsync(int userAId, int userBId)
    {
        var candidateIds = await context.ConversationParticipants
            .Where(p => p.UserId == userAId)
            .Select(p => p.ConversationId)
            .ToListAsync();

        var directConversationIds = await context.Conversations
            .Where(c => c.Type == ConversationType.Direct && candidateIds.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync();

        foreach (var conversationId in directConversationIds)
        {
            var participantIds = await context.ConversationParticipants
                .Where(p => p.ConversationId == conversationId)
                .Select(p => p.UserId)
                .ToListAsync();
            if (participantIds.Count == 2 && participantIds.Contains(userAId) && participantIds.Contains(userBId))
                return await GetByIdAsync(conversationId);
        }
        return null;
    }

    public async Task<Conversation> AddAsync(Conversation conversation)
    {
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();
        return (await GetByIdAsync(conversation.Id))!;
    }

    public async Task<IReadOnlyList<Conversation>> GetActiveForUserAsync(int userId)
    {
        var conversationIds = await context.ConversationParticipants
            .Where(p => p.UserId == userId && p.LeftAt == null)
            .Select(p => p.ConversationId)
            .ToListAsync();
        return await ConversationsWithParticipants.Where(c => conversationIds.Contains(c.Id)).ToListAsync();
    }

    public async Task<IReadOnlyList<Conversation>> SearchAllAsync(string? search)
    {
        var query = ConversationsWithParticipants.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                (c.Title != null && c.Title.ToLower().Contains(term)) ||
                c.Participants.Any(p =>
                    (p.User.Student != null && (p.User.Student.Name.FirstName.ToLower().Contains(term) || p.User.Student.Name.LastName.ToLower().Contains(term) || (p.User.Student.Name.Nickname != null && p.User.Student.Name.Nickname.ToLower().Contains(term)))) ||
                    (p.User.Teacher != null && (p.User.Teacher.Name.FirstName.ToLower().Contains(term) || p.User.Teacher.Name.LastName.ToLower().Contains(term) || (p.User.Teacher.Name.Nickname != null && p.User.Teacher.Name.Nickname.ToLower().Contains(term)))) ||
                    (p.User.Guardian != null && (p.User.Guardian.Name.FirstName.ToLower().Contains(term) || p.User.Guardian.Name.LastName.ToLower().Contains(term) || (p.User.Guardian.Name.Nickname != null && p.User.Guardian.Name.Nickname.ToLower().Contains(term))))));
        }
        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    public async Task<IReadOnlyDictionary<int, Message>> GetLastMessagesAsync(IEnumerable<int> conversationIds)
    {
        var ids = conversationIds.ToList();
        if (ids.Count == 0) return new Dictionary<int, Message>();
        var lastIds = await context.Messages.Where(m => ids.Contains(m.ConversationId)).GroupBy(m => m.ConversationId).Select(g => g.Max(m => m.Id)).ToListAsync();
        var messages = await context.Messages.Where(m => lastIds.Contains(m.Id)).ToListAsync();
        return messages.ToDictionary(m => m.ConversationId);
    }

    public async Task<IReadOnlyDictionary<int, int>> GetUnreadCountsAsync(int userId)
    {
        var query = from p in context.ConversationParticipants
                     where p.UserId == userId && p.LeftAt == null
                     join m in context.Messages on p.ConversationId equals m.ConversationId
                     where m.SenderUserId != userId && (p.LastReadAt == null || m.SentAt > p.LastReadAt)
                     group m by p.ConversationId into g
                     select new { ConversationId = g.Key, Count = g.Count() };
        return (await query.ToListAsync()).ToDictionary(x => x.ConversationId, x => x.Count);
    }

    public Task<int> GetMessageCountAsync(int conversationId) => context.Messages.CountAsync(m => m.ConversationId == conversationId);

    public Task<ConversationParticipant?> GetParticipantAsync(int conversationId, int userId) =>
        context.ConversationParticipants.FirstOrDefaultAsync(p => p.ConversationId == conversationId && p.UserId == userId);

    public async Task<IReadOnlyList<ConversationParticipant>> GetActiveParticipantsAsync(int conversationId) =>
        await context.ConversationParticipants
            .Include(p => p.User).ThenInclude(u => u.Student)
            .Include(p => p.User).ThenInclude(u => u.Teacher)
            .Include(p => p.User).ThenInclude(u => u.Guardian)
            .Where(p => p.ConversationId == conversationId && p.LeftAt == null)
            .ToListAsync();

    public async Task<ConversationParticipant> AddParticipantAsync(int conversationId, int userId, DateTime now)
    {
        var existing = await GetParticipantAsync(conversationId, userId);
        if (existing != null)
        {
            existing.LeftAt = null;
            existing.JoinedAt = now;
            existing.LastReadAt = null;
            await context.SaveChangesAsync();
            return existing;
        }
        var participant = new ConversationParticipant { ConversationId = conversationId, UserId = userId, JoinedAt = now };
        context.ConversationParticipants.Add(participant);
        await context.SaveChangesAsync();
        return participant;
    }

    public async Task RemoveParticipantAsync(ConversationParticipant participant, DateTime now)
    {
        participant.LeftAt = now;
        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Message>> GetMessagesAsync(int conversationId, int? beforeId, int take)
    {
        var query = context.Messages
            .Include(m => m.SenderUser).ThenInclude(u => u.Student)
            .Include(m => m.SenderUser).ThenInclude(u => u.Teacher)
            .Include(m => m.SenderUser).ThenInclude(u => u.Guardian)
            .Where(m => m.ConversationId == conversationId);
        if (beforeId.HasValue) query = query.Where(m => m.Id < beforeId.Value);
        return await query.OrderByDescending(m => m.Id).Take(take).ToListAsync();
    }

    public async Task<Message> AddMessageAsync(Message message)
    {
        context.Messages.Add(message);
        await context.SaveChangesAsync();
        return message;
    }

    public async Task MarkReadAsync(int conversationId, int userId, DateTime readAt)
    {
        var participant = await GetParticipantAsync(conversationId, userId);
        if (participant == null) return;
        participant.LastReadAt = readAt;
        await context.SaveChangesAsync();
    }

    public Task SaveAsync() => context.SaveChangesAsync();
}
