using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using Learna.Api.DTOs;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learna.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IStudentRepository _students;
    private readonly ITeacherRepository _teachers;
    private readonly IGuardianRepository _guardians;
    private readonly IRefreshTokenRepository _refreshTokens;

    public UsersController(
        IUserRepository users,
        IStudentRepository students,
        ITeacherRepository teachers,
        IGuardianRepository guardians,
        IRefreshTokenRepository refreshTokens)
    {
        _users = users;
        _students = students;
        _teachers = teachers;
        _guardians = guardians;
        _refreshTokens = refreshTokens;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ManagedUserDto>>> GetAll([FromQuery] string? search)
    {
        var users = await _users.GetAllAsync(search);
        return Ok(users.Select(ToDto));
    }

    [HttpGet("linkable")]
    public async Task<ActionResult<IEnumerable<LinkablePersonDto>>> GetLinkable([FromQuery] string? linkType)
    {
        var normalized = linkType?.Trim().ToLowerInvariant();
        var result = new List<LinkablePersonDto>();

        if (normalized is null or "teacher")
        {
            var teachers = await _teachers.GetAllAsync();
            result.AddRange(teachers.Where(t => t.User == null).Select(t => new LinkablePersonDto("teacher", t.Id, DisplayName(t.Name), t.Email)));
        }

        if (normalized is null or "student")
        {
            var students = await _students.GetAllAsync();
            result.AddRange(students.Where(s => s.User == null).Select(s => new LinkablePersonDto("student", s.Id, DisplayName(s.Name), s.Email)));
        }

        if (normalized is null or "guardian")
        {
            var guardians = await _guardians.GetAllAsync();
            result.AddRange(guardians.Where(g => g.User == null).Select(g => new LinkablePersonDto("guardian", g.Id, DisplayName(g.Name), g.Email)));
        }

        if (normalized is not null && normalized is not ("teacher" or "student" or "guardian"))
        {
            return BadRequest(new { message = "Link type must be teacher, student, or guardian." });
        }

        return Ok(result.OrderBy(p => p.Name));
    }

    [HttpPost]
    public async Task<ActionResult<ManagedUserDto>> Create(CreateUserDto request)
    {
        var role = NormalizeRole(request.Role);
        var linkType = request.LinkType.Trim().ToLowerInvariant();
        if (role == null || !IsValidLink(role, linkType, request.LinkedId))
        {
            return BadRequest(new { message = "Role and link type do not match." });
        }

        if (!IsValidEmail(request.Email))
        {
            return BadRequest(new { message = "A valid email address is required." });
        }

        if (!IsValidPassword(request.InitialPassword))
        {
            return BadRequest(new { message = "Password must be at least 8 characters." });
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _users.EmailExistsAsync(email))
        {
            return Conflict(new { message = "A user with this email already exists." });
        }

        var user = new User { Email = email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.InitialPassword) };
        if (linkType == "teacher")
        {
            var teacher = await _teachers.GetByIdAsync(request.LinkedId!.Value);
            if (teacher == null) return BadRequest(new { message = "Teacher was not found." });
            if (teacher.User != null) return Conflict(new { message = "This teacher already has a user." });
            user.TeacherId = teacher.Id;
        }
        else if (linkType == "student")
        {
            var student = await _students.GetByIdAsync(request.LinkedId!.Value);
            if (student == null) return BadRequest(new { message = "Student was not found." });
            if (student.User != null) return Conflict(new { message = "This student already has a user." });
            user.StudentId = student.Id;
        }
        else if (linkType == "guardian")
        {
            var guardian = await _guardians.GetByIdAsync(request.LinkedId!.Value);
            if (guardian == null) return BadRequest(new { message = "Guardian was not found." });
            if (guardian.User != null) return Conflict(new { message = "This guardian already has a user." });
            user.GuardianId = guardian.Id;
        }

        var roleEntity = await _users.GetRoleByNameAsync(role);
        if (roleEntity == null) return Problem($"Configured role '{role}' was not found.");
        user.UserRoles.Add(new UserRole { Role = roleEntity, AssignedAt = DateTime.UtcNow });

        var created = await _users.CreateAsync(user);
        var loaded = await _users.GetByIdAsync(created.Id);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, ToDto(loaded!));
    }

    [HttpPut("{id:int}/active")]
    public async Task<ActionResult<ManagedUserDto>> SetActive(int id, SetUserActiveDto request)
    {
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (id == currentUserId && !request.IsActive)
        {
            return BadRequest(new { message = "You cannot deactivate your own account." });
        }

        var user = await _users.GetByIdAsync(id);
        if (user == null) return NotFound();
        user.IsActive = request.IsActive;
        await _users.UpdateAsync(user);
        if (!request.IsActive) await _refreshTokens.RevokeAllUserTokensAsync(id);
        return Ok(ToDto(user));
    }

    [HttpPut("{id:int}/password")]
    public async Task<ActionResult<ResetPasswordResponseDto>> ResetPassword(int id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user == null) return NotFound();
        var temporaryPassword = GenerateTemporaryPassword();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
        await _users.UpdateAsync(user);
        // TODO(WO8): forced change-on-first-login is deliberately deferred.
        return Ok(new ResetPasswordResponseDto(temporaryPassword));
    }

    internal static bool IsValidPassword(string? password) => password?.Length >= 8;

    private static string? NormalizeRole(string role) => role.Trim().ToLowerInvariant() switch
    {
        "admin" => "Admin",
        "teacher" => "Teacher",
        "student" => "Student",
        "parent" => "Parent",
        _ => null
    };

    private static bool IsValidLink(string role, string linkType, int? linkedId) =>
        (role, linkType, linkedId) switch
        {
            ("Admin", "none", null) => true,
            ("Teacher", "teacher", not null) => true,
            ("Student", "student", not null) => true,
            ("Parent", "guardian", not null) => true,
            _ => false
        };

    private static bool IsValidEmail(string email)
    {
        try { return new MailAddress(email.Trim()).Address == email.Trim(); }
        catch { return false; }
    }

    private static string DisplayName(PersonName name) => string.Join(" ", new[] { name.Title, name.FirstName, name.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

    private static ManagedUserDto ToDto(User user)
    {
        var (type, id, name) = user.Teacher != null
            ? ("teacher", (int?)user.Teacher.Id, DisplayName(user.Teacher.Name))
            : user.Student != null
                ? ("student", (int?)user.Student.Id, DisplayName(user.Student.Name))
                : user.Guardian != null
                    ? ("guardian", (int?)user.Guardian.Id, DisplayName(user.Guardian.Name))
                    : ("none", null, (string?)null);
        return new ManagedUserDto(user.Id, user.Email, user.UserRoles.Select(ur => ur.Role.Name).OrderBy(r => r).ToArray(), type, id, name, user.IsActive, user.LastLoginAt);
    }

    private static string GenerateTemporaryPassword()
    {
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string digits = "23456789";
        const string special = "!@$?";
        var all = lower + upper + digits + special;
        var chars = new[] { lower[RandomNumberGenerator.GetInt32(lower.Length)], upper[RandomNumberGenerator.GetInt32(upper.Length)], digits[RandomNumberGenerator.GetInt32(digits.Length)], special[RandomNumberGenerator.GetInt32(special.Length)] }
            .Concat(Enumerable.Range(0, 8).Select(_ => all[RandomNumberGenerator.GetInt32(all.Length)]))
            .OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue));
        return new string(chars.ToArray());
    }
}
