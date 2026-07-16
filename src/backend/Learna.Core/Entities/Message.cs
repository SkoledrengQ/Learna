namespace Learna.Core.Entities;

/// Messages are immutable: no edit or delete endpoints exist, matching the admin-visibility
/// accountability posture (see dev-scope.md Messaging section).
public class Message
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public int SenderUserId { get; set; }
    public User SenderUser { get; set; } = null!;

    /// Plain text, Thai-capable.
    public required string Body { get; set; }
    public DateTime SentAt { get; set; }
}
