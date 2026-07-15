namespace Learna.Core.Entities;

public class AssignmentExtension
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public DateOnly ExtendedDeadlineDate { get; set; }
    public TimeOnly ExtendedDeadlineTime { get; set; }
    public string? Note { get; set; }
}
