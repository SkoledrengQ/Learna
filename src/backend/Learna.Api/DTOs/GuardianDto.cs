namespace Learna.Api.DTOs;

public record StudentGuardianDto(
    int GuardianId,
    PersonNameDto Name,
    string? Email,
    string? PhoneNumber,
    string Relationship,
    bool IsPrimaryContact
);

public record CreateGuardianDto(
    PersonNameDto Name,
    string? Email,
    string? PhoneNumber,
    string Relationship,
    bool IsPrimaryContact
);

public record UpdateGuardianDto(
    PersonNameDto Name,
    string? Email,
    string? PhoneNumber,
    string Relationship,
    bool IsPrimaryContact
);

public record LinkGuardianDto(
    string Relationship,
    bool IsPrimaryContact
);
