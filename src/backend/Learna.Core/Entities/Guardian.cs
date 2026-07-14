namespace Learna.Core.Entities;

public class Guardian
{
    public int Id { get; set; }
    public required PersonName Name { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation property to User (nullable - guardian login is a future work order)
    public User? User { get; set; }

    public ICollection<StudentGuardian> StudentGuardians { get; set; } = new List<StudentGuardian>();
}
