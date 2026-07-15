namespace Learna.Core.Entities;

public class FileResource
{
    public int Id { get; set; }
    public required string OriginalFileName { get; set; }
    public required string StoredPath { get; set; }
    public required string StoredName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public string? Description { get; set; }
    public int UploadedByUserId { get; set; }
    public User UploadedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? SubjectGroupId { get; set; }
    public SubjectGroup? SubjectGroup { get; set; }
    public int? LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    public int? AssignmentId { get; set; }
    public Assignment? Assignment { get; set; }
    public int? SubmissionId { get; set; }
    public Submission? Submission { get; set; }
}
