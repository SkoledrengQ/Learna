namespace Learna.Core.Entities;

// Administrative/homeroom group. Distinct from SubjectGroup (the teaching group) -
// see dev-scope.md "Class Model vs Subject Group Model".
public class SchoolClass
{
    public int Id { get; set; }
    public int SchoolYearId { get; set; }
    public SchoolYear SchoolYear { get; set; } = null!;
    public required string Name { get; set; }  // free text, e.g. "ม.1/1", "1A"
    public string? Description { get; set; }
    public int? HomeroomTeacherId { get; set; }
    public Teacher? HomeroomTeacher { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ClassMembership> Memberships { get; set; } = new List<ClassMembership>();
}
