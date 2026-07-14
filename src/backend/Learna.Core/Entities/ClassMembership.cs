namespace Learna.Core.Entities;

// History-preserving join between Student and SchoolClass. LeftDate == null means
// the membership is currently active. A student may have at most one active
// membership per school year (enforced in the repository/controller, not the DB).
public class ClassMembership
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int ClassId { get; set; }
    public SchoolClass Class { get; set; } = null!;
    public DateTime JoinedDate { get; set; }
    public DateTime? LeftDate { get; set; }
}
