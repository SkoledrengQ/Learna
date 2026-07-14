using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

// Owns rule -> Lesson materialization, rule edit/delete regeneration, and conflict
// detection - see WO5 "Core design decisions" for why lessons are pre-generated rows
// rather than computed on the fly, and dev-scope.md "Schedule / Timetable" for the
// conflict rules (teacher/room/student double-booking).
public interface ILessonSchedulingService
{
    Task<RuleSaveResult> CreateRuleAsync(
        int subjectGroupId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime,
        int? roomId, DateOnly? startDate, DateOnly? endDate, bool force);

    Task<RuleSaveResult> UpdateRuleAsync(
        int ruleId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime,
        int? roomId, DateOnly startDate, DateOnly endDate, bool force);

    Task<bool> DeleteRuleAsync(int ruleId);

    Task<LessonSaveResult> CreateOneOffLessonAsync(
        int subjectGroupId, DateOnly date, TimeOnly startTime, TimeOnly endTime,
        int? roomId, int? teacherId, string? note, bool force);

    Task<LessonSaveResult> UpdateLessonAsync(
        int lessonId, DateOnly date, TimeOnly startTime, TimeOnly endTime,
        int? roomId, int? teacherId, string? note, LessonStatus status, bool force);
}

public class RuleSaveResult
{
    public bool NotFound { get; init; }
    public string? ValidationError { get; init; }
    public LessonRule? Rule { get; init; }
    public IReadOnlyList<LessonConflict> Conflicts { get; init; } = Array.Empty<LessonConflict>();
}

public class LessonSaveResult
{
    public bool NotFound { get; init; }
    public string? ValidationError { get; init; }
    public Lesson? Lesson { get; init; }
    public IReadOnlyList<LessonConflict> Conflicts { get; init; } = Array.Empty<LessonConflict>();
}

public enum ConflictType
{
    Teacher,
    Room,
    Student
}

// One overlap between a candidate lesson time and an existing lesson (existing.LessonId).
public class LessonConflict
{
    public required ConflictType Type { get; init; }
    public required int LessonId { get; init; }
    public required int SubjectGroupId { get; init; }
    public required string SubjectGroupName { get; init; }
    public required DateOnly Date { get; init; }
    public required TimeOnly StartTime { get; init; }
    public required TimeOnly EndTime { get; init; }
    public string? Detail { get; init; }  // conflicting teacher/room/student name(s), for display
}
