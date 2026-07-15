namespace Learna.Core.Entities;

public class Submission
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public DateTime SubmittedAt { get; set; }
    public string? Text { get; set; }
    public bool IsLate { get; set; }
    public ICollection<FileResource> Files { get; set; } = new List<FileResource>();
}
