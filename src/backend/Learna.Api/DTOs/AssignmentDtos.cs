using Learna.Core.Entities;

namespace Learna.Api.DTOs;

public record AssignmentWriteDto(string Title, string? Description, DateOnly? StartDate, DateOnly DeadlineDate, TimeOnly DeadlineTime, LatePolicy LatePolicy);
public record AssignmentFileDto(int Id, string OriginalFileName, string ContentType, long SizeBytes, string? Description, DateTime CreatedAt);
public record AssignmentDto(int Id, int SubjectGroupId, string GroupName, string Title, string? Description, DateOnly? StartDate,
    DateOnly DeadlineDate, TimeOnly DeadlineTime, DateOnly EffectiveDeadlineDate, TimeOnly EffectiveDeadlineTime,
    bool HasExtension, LatePolicy LatePolicy, AssignmentStatus Status, int SubmittedCount, int RosterCount,
    SubmissionDto? OwnSubmission, IReadOnlyList<AssignmentFileDto> Files, DateTime CreatedAt, DateTime UpdatedAt);
public record ExtensionWriteDto(DateOnly ExtendedDeadlineDate, TimeOnly ExtendedDeadlineTime, string? Note);
public record ExtensionDto(int StudentId, DateOnly ExtendedDeadlineDate, TimeOnly ExtendedDeadlineTime, string? Note);
public record SubmissionDto(int Id, int StudentId, DateTime SubmittedAt, string? Text, bool IsLate, IReadOnlyList<AssignmentFileDto> Files, GradeDto? Grade);
public record SubmissionRosterDto(int StudentId, string StudentName, string StudentNumber, string Status, SubmissionDto? Submission, ExtensionDto? Extension);
public record GuardianAssignmentDto(int Id, string Title, int SubjectGroupId, string GroupName, DateOnly EffectiveDeadlineDate,
    TimeOnly EffectiveDeadlineTime, bool HasExtension, string Status, bool IsPast);
public record AssignmentErrorDto(string Code);
