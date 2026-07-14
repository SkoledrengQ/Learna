namespace Learna.Core.Entities;

public class StudentGuardian
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int GuardianId { get; set; }
    public Guardian Guardian { get; set; } = null!;

    public required string Relationship { get; set; }  // e.g. mother/father/guardian
    public bool IsPrimaryContact { get; set; }
}
