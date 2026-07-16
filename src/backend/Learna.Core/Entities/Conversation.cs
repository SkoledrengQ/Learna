namespace Learna.Core.Entities;

public enum ConversationType { Direct = 0, Group = 1 }

public class Conversation
{
    public int Id { get; set; }
    public ConversationType Type { get; set; }

    /// Thai-capable free text; groups only, null for Direct conversations.
    public string? Title { get; set; }
    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
