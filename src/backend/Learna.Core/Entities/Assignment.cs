namespace Learna.Core.Entities;

public enum LatePolicy { Block, AllowMarkLate }
public enum AssignmentStatus { Draft, Published, Closed, Graded, Archived }

public class Assignment
{
    public int Id { get; set; }
    public int SubjectGroupId { get; set; }
    public SubjectGroup SubjectGroup { get; set; } = null!;
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly DeadlineDate { get; set; }
    public TimeOnly DeadlineTime { get; set; }
    public LatePolicy LatePolicy { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Draft;
    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<AssignmentExtension> Extensions { get; set; } = new List<AssignmentExtension>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public ICollection<FileResource> Files { get; set; } = new List<FileResource>();
}
