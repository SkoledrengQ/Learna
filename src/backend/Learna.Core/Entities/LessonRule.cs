namespace Learna.Core.Entities;

// Recurring weekly timetable rule for a SubjectGroup. Materializes into Lesson rows
// at save time (one per matching weekday in [StartDate, EndDate]) - see dev-scope.md
// "Schedule / Timetable" and WO5 for why lessons are materialized rather than computed
// on the fly. Editing/deleting a rule only touches its own future, unmodified lessons.
public class LessonRule
{
    public int Id { get; set; }
    public int SubjectGroupId { get; set; }
    public SubjectGroup SubjectGroup { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int? RoomId { get; set; }
    public Room? Room { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
