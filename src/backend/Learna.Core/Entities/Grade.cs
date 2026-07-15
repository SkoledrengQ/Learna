namespace Learna.Core.Entities;

public enum GradeStatus { Draft, Published }

public class Grade
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int SubjectGroupId { get; set; }
    public SubjectGroup SubjectGroup { get; set; } = null!;
    public int? SubmissionId { get; set; }
    public Submission? Submission { get; set; }
    public required string Category { get; set; }
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; } = 100;
    public string? Feedback { get; set; }
    public GradeStatus Status { get; set; } = GradeStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public int GradedByUserId { get; set; }
    public User GradedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
