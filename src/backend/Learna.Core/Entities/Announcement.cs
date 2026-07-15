namespace Learna.Core.Entities;

public enum AnnouncementAudienceType
{
    School = 0,
    SchoolClass = 1,
    SubjectGroup = 2
}

public class Announcement
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public AnnouncementAudienceType AudienceType { get; set; }
    public int? TargetId { get; set; }
    public DateTime PublishAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<AnnouncementRead> Reads { get; set; } = new List<AnnouncementRead>();
    public ICollection<FileResource> Files { get; set; } = new List<FileResource>();
}
