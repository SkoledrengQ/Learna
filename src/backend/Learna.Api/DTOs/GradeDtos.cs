using Learna.Core.Entities;

namespace Learna.Api.DTOs;

public record GradeWriteDto(decimal Score, decimal MaxScore = 100, string? Feedback = null, string? Category = null);
public record ManualGradeWriteDto(int StudentId, string Category, decimal Score, decimal MaxScore = 100, string? Feedback = null);
public record GradeDto(int Id, int StudentId, string StudentName, int SubjectGroupId, int? SubmissionId,
    int? AssignmentId, string Category, decimal Score, decimal MaxScore, string? Feedback, GradeStatus Status,
    DateTime? PublishedAt, DateTime CreatedAt, DateTime UpdatedAt);
public record GradeGroupDto(int SubjectGroupId, string GroupName, string SubjectName, decimal AveragePercentage,
    IReadOnlyList<GradeDto> Grades);
public record GradeErrorDto(string Code);
