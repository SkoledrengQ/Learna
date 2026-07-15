namespace Learna.Core.Entities;

public class Student
{
    public int Id { get; set; }
    public required PersonName Name { get; set; }
    public required string Email { get; set; }
    public required string StudentId { get; set; }  // Unique student identifier
    public required string IdCardNumber { get; set; }  // National ID card number
    public DateTime DateOfBirth { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public required string PhoneNumber { get; set; }
    public required string Address { get; set; }
    public decimal? Height { get; set; }  // cm
    public decimal? Weight { get; set; }  // kg
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation property to User (nullable - student might not have login yet)
    public User? User { get; set; }

    public ICollection<StudentGuardian> StudentGuardians { get; set; } = new List<StudentGuardian>();
    public ICollection<ClassMembership> ClassMemberships { get; set; } = new List<ClassMembership>();
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
