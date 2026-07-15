namespace Learna.Api.DTOs;

public record TeacherDto(
    int Id,
    PersonNameDto Name,
    string? Email,
    string? PhoneNumber,
    string? EmployeeId
);

public record CreateTeacherDto(
    PersonNameDto Name,
    string Email,
    string? PhoneNumber,
    string? EmployeeId
);

public record UpdateTeacherDto(
    PersonNameDto Name,
    string Email,
    string? PhoneNumber,
    string? EmployeeId
);
