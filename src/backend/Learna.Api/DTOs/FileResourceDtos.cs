namespace Learna.Api.DTOs;

public record FileResourceDto(int Id, string OriginalFileName, string ContentType, long SizeBytes, string? Description,
    int UploadedByUserId, string UploadedBy, DateTime CreatedAt, int SubjectGroupId, string SubjectGroupName,
    int? LessonId, DateOnly? LessonDate, bool CanDelete, int? ChildId = null, string? ChildName = null);

public record FileErrorDto(string Code);
