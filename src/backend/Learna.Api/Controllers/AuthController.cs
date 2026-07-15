using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Learna.Core.Interfaces;
using Learna.Api.DTOs;
using System.Security.Claims;

namespace Learna.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password);

        if (!result.Success)
        {
            return Unauthorized(new { message = result.ErrorMessage });
        }

        // Set refresh token in httpOnly cookie
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // HTTPS only in production
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        Response.Cookies.Append("refreshToken", result.RefreshToken!, cookieOptions);

        var response = new LoginResponseDto(
            result.AccessToken!,
            result.RefreshToken!,
            result.ExpiresAt!.Value,
            new UserDto(
                result.User!.Id,
                result.User.Email,
                result.User.Roles,
                result.User.StudentId,
                result.User.GuardianId,
                result.User.PreferredLanguage
            )
        );

        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> RefreshToken()
    {
        // Get refresh token from cookie
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
        {
            return Unauthorized(new { message = "Refresh token not found" });
        }

        var result = await _authService.RefreshTokenAsync(refreshToken);

        if (!result.Success)
        {
            return Unauthorized(new { message = result.ErrorMessage });
        }

        // Update refresh token cookie
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        Response.Cookies.Append("refreshToken", result.RefreshToken!, cookieOptions);

        var response = new LoginResponseDto(
            result.AccessToken!,
            result.RefreshToken!,
            result.ExpiresAt!.Value,
            new UserDto(
                result.User!.Id,
                result.User.Email,
                result.User.Roles,
                result.User.StudentId,
                result.User.GuardianId,
                result.User.PreferredLanguage
            )
        );

        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
        {
            await _authService.RevokeTokenAsync(refreshToken);
            Response.Cookies.Delete("refreshToken");
        }

        return Ok(new { message = "Logged out successfully" });
    }

    [HttpPut("password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        if (!UsersController.IsValidPassword(request.NewPassword))
        {
            return BadRequest(new { message = "Password must be at least 8 characters." });
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);

        if (!result)
        {
            return BadRequest(new { message = "Current password is incorrect" });
        }

        return Ok(new { message = "Password changed successfully" });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var email = User.FindFirstValue(ClaimTypes.Email)!;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var studentIdClaim = User.FindFirstValue("StudentId");
        var studentId = studentIdClaim != null ? int.Parse(studentIdClaim) : (int?)null;
        var guardianIdClaim = User.FindFirstValue("GuardianId");
        var guardianId = guardianIdClaim != null ? int.Parse(guardianIdClaim) : (int?)null;

        return Ok(new UserDto(userId, email, roles, studentId, guardianId, null));
    }

    /// Self-service: any authenticated role may update their own language preference.
    [HttpPut("language")]
    [Authorize]
    public async Task<ActionResult<UserDto>> UpdateLanguagePreference([FromBody] UpdateLanguagePreferenceRequestDto request)
    {
        if (request.Language != "en" && request.Language != "th")
        {
            return BadRequest(new { message = "Language must be 'en' or 'th'" });
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var updatedUser = await _authService.UpdateLanguagePreferenceAsync(userId, request.Language);

        if (updatedUser == null)
        {
            return NotFound();
        }

        return Ok(new UserDto(updatedUser.Id, updatedUser.Email, updatedUser.Roles, updatedUser.StudentId, updatedUser.GuardianId, updatedUser.PreferredLanguage));
    }
}
