namespace Learna.Core.Entities;

public class Subject
{
    public int Id { get; set; }
    public required string Code { get; set; }  // internal id, e.g. "math"
    public required string NameEnglish { get; set; }
    public string? NameThai { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
