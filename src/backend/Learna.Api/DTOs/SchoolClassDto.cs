namespace Learna.Api.DTOs;

public record SchoolClassDto(
    int Id,
    int SchoolYearId,
    string Name,
    string? Description,
    int? HomeroomTeacherId
);

public record CreateSchoolClassDto(
    int SchoolYearId,
    string Name,
    string? Description,
    int? HomeroomTeacherId
);

public record UpdateSchoolClassDto(
    string Name,
    string? Description,
    int? HomeroomTeacherId
);

public record ClassMembershipDto(
    int Id,
    StudentSummaryDto Student,
    DateTime JoinedDate,
    DateTime? LeftDate
);

public record AddClassMemberDto(
    int StudentId,
    DateTime? JoinedDate
);

public record MoveClassMemberDto(
    int ToClassId,
    DateTime? MoveDate
);
