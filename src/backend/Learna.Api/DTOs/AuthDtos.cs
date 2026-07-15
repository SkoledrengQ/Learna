namespace Learna.Api.DTOs;

public record LoginRequestDto(
    string Email,
    string Password
);

public record LoginResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);

public record UserDto(
    int Id,
    string Email,
    List<string> Roles,
    int? StudentId,
    int? GuardianId,
    string? PreferredLanguage
);

public record RefreshTokenRequestDto(
    string RefreshToken
);

public record ChangePasswordRequestDto(
    string CurrentPassword,
    string NewPassword
);

public record UpdateLanguagePreferenceRequestDto(
    string Language
);
