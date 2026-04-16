namespace Learna.Core.Entities;

public class Student
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string StudentId { get; set; }  // Unique student identifier
    public required string IdCardNumber { get; set; }  // National ID card number
    public DateTime DateOfBirth { get; set; }
    public required string ParentPhoneNumber { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public int GradeLevel { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation property to User (nullable - student might not have login yet)
    public User? User { get; set; }
}
