using System.Reflection;
using System.Security.Claims;
using FluentAssertions;
using Learna.Api.Controllers;
using Learna.Api.DTOs;
using Learna.Core.Entities;
using Learna.Infrastructure.Data;
using Learna.Infrastructure.Repositories;
using Learna.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Learna.Tests;

public class UserAccountManagementTests
{
    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static TokenService Tokens() => new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Jwt:SecretKey"] = "a-development-test-secret-that-is-at-least-32-characters",
        ["Jwt:Issuer"] = "LearnaApi",
        ["Jwt:Audience"] = "LearnaApp"
    }).Build());

    private static UsersController Users(ApplicationDbContext db, int currentUserId = 1) => WithUser(
        new UsersController(new UserRepository(db), new StudentRepository(db), new TeacherRepository(db), new GuardianRepository(db), new RefreshTokenRepository(db)),
        currentUserId,
        "Admin");

    private static T WithUser<T>(T controller, int userId, string role) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, role)
                }, "test"))
            }
        };
        return controller;
    }

    [Fact]
    public void AuthorizationAndRoutes_MatchRoleMatrix()
    {
        typeof(UsersController).GetCustomAttributes<AuthorizeAttribute>().Single().Roles.Should().Be("Admin");
        typeof(StudentsController).GetCustomAttributes<AuthorizeAttribute>().Single().Roles.Should().Be("Admin,Teacher");

        var password = typeof(AuthController).GetMethod(nameof(AuthController.ChangePassword))!;
        password.GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
        password.GetCustomAttributes<HttpMethodAttribute>().Single().HttpMethods.Should().Equal("PUT");
        password.GetCustomAttributes<HttpMethodAttribute>().Single().Template.Should().Be("password");

        var studentSchedule = typeof(ScheduleController).GetMethod(nameof(ScheduleController.GetStudentSchedule))!;
        studentSchedule.GetCustomAttribute<AuthorizeAttribute>()!.Roles.Should().Be("Admin,Teacher,Student,Parent");
        typeof(ScheduleController).GetMethod(nameof(ScheduleController.GetTeacherSchedule))!.GetCustomAttribute<AuthorizeAttribute>()!.Roles.Should().Be("Admin,Teacher");
        typeof(ScheduleController).GetMethod(nameof(ScheduleController.GetRoomSchedule))!.GetCustomAttribute<AuthorizeAttribute>()!.Roles.Should().Be("Admin,Teacher");

        // ASP.NET's authorization middleware challenges unauthenticated callers (401)
        // and forbids authenticated callers outside these role lists (403).
        typeof(UsersController).GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
        typeof(AuthController).GetCustomAttribute<AllowAnonymousAttribute>().Should().BeNull();
    }

    [Fact]
    public async Task Create_RejectsRoleLinkMismatch_AndDoubleLink()
    {
        await using var db = Context();
        db.Roles.Add(new Role { Id = 3, Name = "Student", Description = "Student" });
        var student = Student("linked@test", "S1");
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var controller = Users(db);
        (await controller.Create(new CreateUserDto("new@test.com", "Password1!", "Teacher", "student", student.Id))).Result.Should().BeOfType<BadRequestObjectResult>();

        db.Users.Add(new User { Email = "existing@test.com", PasswordHash = "x", StudentId = student.Id });
        await db.SaveChangesAsync();
        (await controller.Create(new CreateUserDto("new@test.com", "Password1!", "Student", "student", student.Id))).Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CreateListDeactivateReset_SearchAndSelfProtection_Work()
    {
        await using var db = Context();
        db.Roles.AddRange(
            new Role { Id = 1, Name = "Admin", Description = "Admin" },
            new Role { Id = 2, Name = "Teacher", Description = "Teacher" });
        var admin = new User { Email = "admin@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!") };
        var teacher = new Teacher { Name = new PersonName { FirstName = "Wipa", LastName = "Jaidee" }, Email = "wipa@school.test" };
        db.AddRange(admin, teacher);
        await db.SaveChangesAsync();
        var controller = Users(db, admin.Id);

        var createdResult = await controller.Create(new CreateUserDto("Login@School.Test", "Teacher123!", "Teacher", "teacher", teacher.Id));
        var created = ((CreatedAtActionResult)createdResult.Result!).Value.Should().BeOfType<ManagedUserDto>().Subject;
        created.Email.Should().Be("login@school.test");
        created.LinkedName.Should().Contain("Wipa");

        var list = ((OkObjectResult)(await controller.GetAll("wipa")).Result!).Value.Should().BeAssignableTo<IEnumerable<ManagedUserDto>>().Subject;
        list.Should().ContainSingle(u => u.Id == created.Id);
        (await controller.SetActive(admin.Id, new SetUserActiveDto(false))).Result.Should().BeOfType<BadRequestObjectResult>();

        var deactivated = ((OkObjectResult)(await controller.SetActive(created.Id, new SetUserActiveDto(false))).Result!).Value.Should().BeOfType<ManagedUserDto>().Subject;
        deactivated.IsActive.Should().BeFalse();
        var reset = ((OkObjectResult)(await controller.ResetPassword(created.Id)).Result!).Value.Should().BeOfType<ResetPasswordResponseDto>().Subject;
        reset.TemporaryPassword.Should().HaveLength(12);
        BCrypt.Net.BCrypt.Verify(reset.TemporaryPassword, (await db.Users.FindAsync(created.Id))!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task DeactivatedUser_IsBlockedAtLoginAndRefresh()
    {
        await using var db = Context();
        var user = new User { Email = "inactive@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"), IsActive = false };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var token = new RefreshToken { Token = "still-live", UserId = user.Id, ExpiresAt = DateTime.UtcNow.AddDays(1) };
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();
        var auth = new AuthService(new UserRepository(db), new RefreshTokenRepository(db), Tokens());

        (await auth.LoginAsync(user.Email, "Password1!")).Success.Should().BeFalse();
        (await auth.RefreshTokenAsync(token.Token)).Success.Should().BeFalse();
    }

    [Fact]
    public async Task SelfPasswordChange_RejectsWrongCurrent_ValidatesNew_AndUpdatesHash()
    {
        await using var db = Context();
        var user = new User { Email = "student@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass1!") };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var auth = new AuthService(new UserRepository(db), new RefreshTokenRepository(db), Tokens());
        var controller = WithUser(new AuthController(auth, new SchoolSettingsRepository(db), NullLogger<AuthController>.Instance), user.Id, "Student");

        (await controller.ChangePassword(new ChangePasswordRequestDto("wrong", "NewPass1!"))).Should().BeOfType<BadRequestObjectResult>();
        (await controller.ChangePassword(new ChangePasswordRequestDto("OldPass1!", "short"))).Should().BeOfType<BadRequestObjectResult>();
        (await controller.ChangePassword(new ChangePasswordRequestDto("OldPass1!", "NewPass1!"))).Should().BeOfType<OkObjectResult>();
        BCrypt.Net.BCrypt.Verify("NewPass1!", (await db.Users.FindAsync(user.Id))!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task StudentSchedule_AllowsOwnAndForbidsAnother_TeacherRosterStillReads()
    {
        await using var db = Context();
        var own = Student("own@test", "S1");
        var other = Student("other@test", "S2");
        db.AddRange(own, other);
        await db.SaveChangesAsync();
        var user = new User { Email = "own-login@test", PasswordHash = "x", StudentId = own.Id };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var schedule = WithUser(new ScheduleController(new LessonRepository(db), new EnrollmentRepository(db), new StudentRepository(db), new TeacherRepository(db), new RoomRepository(db), new UserRepository(db), new GuardianRepository(db)), user.Id, "Student");

        (await schedule.GetStudentSchedule(own.Id, null, null)).Result.Should().BeOfType<OkObjectResult>();
        (await schedule.GetStudentSchedule(other.Id, null, null)).Result.Should().BeOfType<ForbidResult>();

        var roster = WithUser(new StudentsController(new StudentRepository(db), new GuardianRepository(db), NullLogger<StudentsController>.Instance), 999, "Teacher");
        (await roster.GetAll()).Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TeacherReferenceRead_HidesPiiFromNonAdmin()
    {
        await using var db = Context();
        db.Teachers.Add(new Teacher { Name = new PersonName { FirstName = "Wipa", LastName = "Jaidee" }, Email = "private@test", PhoneNumber = "0123", EmployeeId = "E1" });
        await db.SaveChangesAsync();
        var controller = WithUser(new TeachersController(new TeacherRepository(db)), 10, "Student");
        var teachers = ((OkObjectResult)(await controller.GetAll()).Result!).Value.Should().BeAssignableTo<IEnumerable<TeacherDto>>().Subject;
        teachers.Single().Should().Match<TeacherDto>(t => t.Email == null && t.PhoneNumber == null && t.EmployeeId == null);
    }

    private static Student Student(string email, string studentId) => new()
    {
        Name = new PersonName { FirstName = studentId, LastName = "Student" }, Email = email, StudentId = studentId,
        IdCardNumber = $"ID-{studentId}", DateOfBirth = DateTime.Today.AddYears(-12), EnrollmentDate = DateTime.Today,
        PhoneNumber = "0", Address = "Bangkok"
    };
}
