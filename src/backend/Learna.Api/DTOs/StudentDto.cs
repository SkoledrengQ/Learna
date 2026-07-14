namespace Learna.Api.DTOs;

public record PersonNameDto(
    string? Title,
    string FirstName,
    string LastName,
    string? FirstNameEnglish,
    string? LastNameEnglish,
    string? Nickname
);

public record StudentDto(
    int Id,
    PersonNameDto Name,
    string Email,
    string StudentId,
    string IdCardNumber,
    DateTime DateOfBirth,
    DateTime EnrollmentDate,
    string PhoneNumber,
    string Address,
    decimal? Height,
    decimal? Weight
);

public record CreateStudentDto(
    PersonNameDto Name,
    string Email,
    string IdCardNumber,
    DateTime DateOfBirth,
    DateTime EnrollmentDate,
    string PhoneNumber,
    string Address,
    decimal? Height,
    decimal? Weight
);

public record UpdateStudentDto(
    PersonNameDto Name,
    string Email,
    string IdCardNumber,
    DateTime DateOfBirth,
    string PhoneNumber,
    string Address,
    decimal? Height,
    decimal? Weight
);
