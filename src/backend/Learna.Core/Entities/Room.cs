namespace Learna.Core.Entities;

public class Room
{
    public int Id { get; set; }
    public required string Name { get; set; }  // free text, Thai-capable, e.g. "ห้อง 204", "Science Lab"
    public string? Building { get; set; }
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
