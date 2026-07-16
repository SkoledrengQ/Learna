namespace Learna.Api.DTOs;

public record SchoolSettingsDto(
    string SchoolName,
    string PrimaryColor
);

public record UpdateSchoolSettingsDto(
    string SchoolName,
    string PrimaryColor
);
