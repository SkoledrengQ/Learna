namespace Learna.Core.Entities;

// Teaching group - who actually sits in a lesson. Deliberately has no required link
// to a Class: its roster is defined purely by Enrollments, which is what lets a
// group span multiple classes or cover only some students from one class.
// TODO: "auto-enroll new students joining a class" flag from dev-scope.md is
// deliberately deferred - not implemented here.
public class SubjectGroup
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
    public int TermId { get; set; }
    public Term Term { get; set; } = null!;
    public required string Name { get; set; }  // free text, e.g. "1A - Mathematics", "Spanish Beginner Group"
    public int? TeacherId { get; set; }
    public Teacher? Teacher { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
