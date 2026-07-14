namespace Learna.Api.DTOs;

public record SubjectDto(
    int Id,
    string Code,
    string NameEnglish,
    string? NameThai,
    string? Description
);

public record CreateSubjectDto(
    string Code,
    string NameEnglish,
    string? NameThai,
    string? Description
);

public record UpdateSubjectDto(
    string Code,
    string NameEnglish,
    string? NameThai,
    string? Description
);
