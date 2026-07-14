using Learna.Core.Entities;

namespace Learna.Api.DTOs;

public record LessonRuleDto(
    int Id,
    int SubjectGroupId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int? RoomId,
    string? RoomName,
    DateOnly StartDate,
    DateOnly EndDate
);

public record CreateLessonRuleDto(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int? RoomId,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool Force = false
);

public record UpdateLessonRuleDto(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int? RoomId,
    DateOnly StartDate,
    DateOnly EndDate,
    bool Force = false
);
