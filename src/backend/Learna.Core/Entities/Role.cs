namespace Learna.Core.Entities;

public class Role
{
    public int Id { get; set; }
    public required string Name { get; set; } // "Admin", "Teacher", "Student", "Parent"
    public required string Description { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
