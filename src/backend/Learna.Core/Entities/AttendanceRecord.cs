namespace Learna.Core.Entities;

public class AttendanceRecord
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public AttendanceStatus Status { get; set; }
    public string? Note { get; set; }
    public int RecordedByUserId { get; set; }
    public User RecordedByUser { get; set; } = null!;
    public DateTime RecordedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
