namespace Learna.Api.DTOs;

public record SubjectGroupDto(
    int Id,
    int SubjectId,
    int TermId,
    string Name,
    int? TeacherId
);

public record CreateSubjectGroupDto(
    int SubjectId,
    int TermId,
    string Name,
    int? TeacherId
);

public record UpdateSubjectGroupDto(
    string Name,
    int? TeacherId
);

public record EnrollmentDto(
    int Id,
    StudentSummaryDto Student,
    DateTime EnrolledDate,
    DateTime? UnenrolledDate
);

public record EnrollStudentDto(
    int StudentId,
    DateTime? EnrolledDate
);

public record EnrollClassResultDto(int Enrolled, int Skipped);
