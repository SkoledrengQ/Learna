using Learna.Core.Entities;

namespace Learna.Api.DTOs;

public record AnnouncementFileDto(int Id, string OriginalFileName, string ContentType, long SizeBytes, string? Description, DateTime CreatedAt);
public record AnnouncementDto(int Id, string Title, string Body, int CreatedByUserId, string AuthorName, AnnouncementAudienceType AudienceType, int? TargetId, string AudienceLabel, DateTime PublishAt, DateTime? ExpiresAt, DateTime CreatedAt, DateTime? UpdatedAt, bool IsRead, bool IsFuture, bool IsExpired, bool CanEdit, IReadOnlyList<AnnouncementFileDto> Files);
public record AnnouncementFeedDto(IReadOnlyList<AnnouncementDto> Announcements, int UnreadCount);
public record AnnouncementWriteDto(string Title, string Body, AnnouncementAudienceType AudienceType, int? TargetId, DateTime? PublishAt, DateTime? ExpiresAt);
public record AnnouncementTargetDto(AnnouncementAudienceType AudienceType, int? TargetId, string Label);
public record AnnouncementTargetsDto(IReadOnlyList<AnnouncementTargetDto> Targets);
public record AnnouncementErrorDto(string Code);
