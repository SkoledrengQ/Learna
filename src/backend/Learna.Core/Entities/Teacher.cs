namespace Learna.Core.Entities;

public class Teacher
{
    public int Id { get; set; }
    public required PersonName Name { get; set; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? EmployeeId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation property to User (nullable - teacher login is a later work order)
    public User? User { get; set; }
}
