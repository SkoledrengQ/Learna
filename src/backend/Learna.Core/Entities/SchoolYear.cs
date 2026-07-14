namespace Learna.Core.Entities;

public class SchoolYear
{
    public int Id { get; set; }
    public required string Name { get; set; }  // free text, e.g. "2026/2027" or Thai Buddhist Era "2569"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Term> Terms { get; set; } = new List<Term>();
}
