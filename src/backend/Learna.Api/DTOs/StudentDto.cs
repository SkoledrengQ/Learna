namespace Learna.Api.DTOs;

public record StudentDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string StudentId,
    string IdCardNumber,
    DateTime DateOfBirth,
    string ParentPhoneNumber,
    DateTime EnrollmentDate,
    int GradeLevel
);

public record CreateStudentDto(
    string FirstName,
    string LastName,
    string Email,
    string IdCardNumber,
    DateTime DateOfBirth,
    string ParentPhoneNumber,
    DateTime EnrollmentDate,
    int GradeLevel
);

public record UpdateStudentDto(
    string FirstName,
    string LastName,
    string Email,
    string IdCardNumber,
    DateTime DateOfBirth,
    string ParentPhoneNumber,
    int GradeLevel
);
