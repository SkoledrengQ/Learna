using Learna.Core.Entities;

namespace Learna.Api.DTOs;

public record ParticipantDto(int UserId, string Name, IReadOnlyList<string> Roles, DateTime JoinedAt, DateTime? LeftAt, bool IsCreator);
public record ConversationSummaryDto(int Id, ConversationType Type, string DisplayTitle, IReadOnlyList<string> ParticipantNames, string? LastMessagePreview, DateTime? LastMessageAt, int UnreadCount, bool IsCreator);
public record ConversationListDto(IReadOnlyList<ConversationSummaryDto> Conversations, int TotalUnread);
public record ConversationDetailDto(int Id, ConversationType Type, string DisplayTitle, int CreatedByUserId, bool IsCreator, IReadOnlyList<ParticipantDto> Participants);
public record CreateConversationDto(ConversationType Type, int? UserId, string? Title, List<int>? UserIds);
public record MessageDto(int Id, int SenderUserId, string SenderName, string Body, DateTime SentAt, bool IsMine);
public record MessagePageDto(IReadOnlyList<MessageDto> Messages, bool HasMore);
public record SendMessageDto(string Body);
public record AddParticipantsDto(List<int> UserIds);
public record ConversationErrorDto(string Code);

public record DirectoryPersonDto(int UserId, string Name, IReadOnlyList<string> Roles);

public record AdminConversationSummaryDto(int Id, ConversationType Type, string DisplayTitle, IReadOnlyList<string> ParticipantNames, int MessageCount, DateTime? LastMessageAt, DateTime CreatedAt);

public record MessagingPolicyRuleDto(string RoleA, string RoleB, bool Allowed);
public record MessagingPolicyDto(IReadOnlyList<MessagingPolicyRuleDto> Rules);
public record UpdateMessagingPolicyDto(IReadOnlyList<MessagingPolicyRuleDto> Rules);
