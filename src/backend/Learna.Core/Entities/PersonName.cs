namespace Learna.Core.Entities;

public class PersonName
{
    public string? Title { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? FirstNameEnglish { get; set; }
    public string? LastNameEnglish { get; set; }
    public string? Nickname { get; set; }
}
