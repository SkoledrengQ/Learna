namespace Learna.Api.DTOs;

public record CreateUserDto(
    string Email,
    string InitialPassword,
    string Role,
    string LinkType,
    int? LinkedId
);

public record ManagedUserDto(
    int Id,
    string Email,
    IReadOnlyCollection<string> Roles,
    string LinkType,
    int? LinkedId,
    string? LinkedName,
    bool IsActive,
    DateTime? LastLoginAt
);

public record LinkablePersonDto(
    string LinkType,
    int Id,
    string Name,
    string? Email
);

public record SetUserActiveDto(bool IsActive);
public record ResetPasswordResponseDto(string TemporaryPassword);
