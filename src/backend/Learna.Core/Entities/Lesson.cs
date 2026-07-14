namespace Learna.Core.Entities;

// A concrete, materialized class session. Either generated from a LessonRule
// (SourceRuleId set) or created as a one-off (SourceRuleId null). Exceptions to a
// recurring rule (cancel, move, room/teacher change) are edits to the individual
// Lesson row, never to the rule - see WO5 "Core design decisions".
public class Lesson
{
    public int Id { get; set; }
    public int SubjectGroupId { get; set; }
    public SubjectGroup SubjectGroup { get; set; } = null!;
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int? RoomId { get; set; }
    public Room? Room { get; set; }
    public int? TeacherId { get; set; }  // copied from SubjectGroup at generation; overwritten for a substitute
    public Teacher? Teacher { get; set; }
    public LessonStatus Status { get; set; } = LessonStatus.Scheduled;
    public string? Note { get; set; }
    public int? SourceRuleId { get; set; }  // null = one-off, manually created lesson
    public LessonRule? SourceRule { get; set; }
    public bool IsModified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
