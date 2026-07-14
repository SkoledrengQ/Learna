namespace Learna.Core.Entities;

public class Term
{
    public int Id { get; set; }
    public int SchoolYearId { get; set; }
    public SchoolYear SchoolYear { get; set; } = null!;
    public required string Name { get; set; }  // free text, e.g. "Semester 1", "ภาคเรียนที่ 1"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
