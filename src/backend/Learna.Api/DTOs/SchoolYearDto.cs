namespace Learna.Api.DTOs;

public record SchoolYearDto(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    bool IsArchived
);

public record CreateSchoolYearDto(
    string Name,
    DateTime StartDate,
    DateTime EndDate
);

public record UpdateSchoolYearDto(
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    bool IsArchived
);

public record TermDto(
    int Id,
    int SchoolYearId,
    string Name,
    DateTime StartDate,
    DateTime EndDate
);

public record CreateTermDto(
    string Name,
    DateTime StartDate,
    DateTime EndDate
);

public record UpdateTermDto(
    string Name,
    DateTime StartDate,
    DateTime EndDate
);
