namespace Learna.Core.Entities;

/// Single-row singleton (one row semantics enforced by the repository, not the schema).
public class SchoolSettings
{
    public int Id { get; set; }
    public required string SchoolName { get; set; }  // Thai-capable free text
    public required string PrimaryColor { get; set; }  // "#RRGGBB"
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
