using Learna.Core.Entities;
using Learna.Core.Interfaces;

namespace Learna.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user == null || !user.IsActive)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Invalid email or password"
            };
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Invalid email or password"
            };
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        // Get user roles
        var roles = await _userRepository.GetUserRolesAsync(user.Id);

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(
            user.Id,
            user.Email,
            roles,
            user.StudentId
        );

        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _refreshTokenRepository.CreateAsync(refreshToken);

        return new AuthResult
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Roles = roles.ToList(),
                StudentId = user.StudentId,
                PreferredLanguage = user.PreferredLanguage
            }
        };
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

        if (storedToken == null || !storedToken.IsActive || !storedToken.User.IsActive)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Invalid or expired refresh token"
            };
        }

        var user = storedToken.User;
        var roles = await _userRepository.GetUserRolesAsync(user.Id);

        // Generate new access token
        var accessToken = _tokenService.GenerateAccessToken(
            user.Id,
            user.Email,
            roles,
            user.StudentId
        );

        // Generate new refresh token
        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();
        var newRefreshToken = new RefreshToken
        {
            Token = newRefreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Revoke old token
        await _refreshTokenRepository.RevokeAsync(refreshToken);

        // Save new token
        await _refreshTokenRepository.CreateAsync(newRefreshToken);

        return new AuthResult
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = newRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Roles = roles.ToList(),
                StudentId = user.StudentId,
                PreferredLanguage = user.PreferredLanguage
            }
        };
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        await _refreshTokenRepository.RevokeAsync(refreshToken);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            return false;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _userRepository.UpdateAsync(user);

        return true;
    }

    public async Task<UserInfo?> UpdateLanguagePreferenceAsync(int userId, string language)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        user.PreferredLanguage = language;
        await _userRepository.UpdateAsync(user);

        var roles = await _userRepository.GetUserRolesAsync(user.Id);

        return new UserInfo
        {
            Id = user.Id,
            Email = user.Email,
            Roles = roles.ToList(),
            StudentId = user.StudentId,
            PreferredLanguage = user.PreferredLanguage
        };
    }
}
