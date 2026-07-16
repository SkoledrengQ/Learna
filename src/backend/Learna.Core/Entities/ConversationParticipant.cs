namespace Learna.Core.Entities;

public class ConversationParticipant
{
    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime JoinedAt { get; set; }

    /// Set when the participant leaves a group; Direct participants never leave. A departed
    /// member loses access to the conversation (including history) but the conversation
    /// keeps functioning for remaining members.
    public DateTime? LeftAt { get; set; }

    /// Drives the caller's own unread count; never exposed to other participants (no read receipts).
    public DateTime? LastReadAt { get; set; }
}
