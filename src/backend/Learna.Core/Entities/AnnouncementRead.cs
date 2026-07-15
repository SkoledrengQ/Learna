namespace Learna.Core.Entities;

public class AnnouncementRead
{
    public int Id { get; set; }
    public int AnnouncementId { get; set; }
    public Announcement Announcement { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime ReadAt { get; set; }
}
