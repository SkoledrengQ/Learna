namespace Learna.Core.Entities;

// History-preserving join between Student and SubjectGroup. UnenrolledDate == null
// means the enrollment is currently active. A student may have at most one active
// enrollment per subject group (enforced in the repository/controller, not the DB).
public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int SubjectGroupId { get; set; }
    public SubjectGroup SubjectGroup { get; set; } = null!;
    public DateTime EnrolledDate { get; set; }
    public DateTime? UnenrolledDate { get; set; }
}
