using System.Security.Claims;
using FluentAssertions;
using Learna.Api.Controllers;
using Learna.Api.DTOs;
using Learna.Core.Entities;
using Learna.Infrastructure.Data;
using Learna.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Learna.Tests;

public class GuardianPortalTests
{
    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static IConfiguration Config() => new ConfigurationBuilder().Build();

    private static T As<T>(T controller, User user, params string[] roles) where T : ControllerBase
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id.ToString()) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) }
        };
        return controller;
    }

    private static ScheduleController Schedule(ApplicationDbContext db, User user, params string[] roles) => As(
        new ScheduleController(new LessonRepository(db), new EnrollmentRepository(db), new StudentRepository(db),
            new TeacherRepository(db), new RoomRepository(db), new UserRepository(db), new GuardianRepository(db)),
        user, roles);

    private static AttendanceController Attendance(ApplicationDbContext db, User user, params string[] roles) => As(
        new AttendanceController(new LessonRepository(db), new AttendanceRepository(db), new UserRepository(db),
            new StudentRepository(db), new GuardianRepository(db), Config()), user, roles);

    private static GuardiansController Guardians(ApplicationDbContext db, User user) => As(
        new GuardiansController(new UserRepository(db), new GuardianRepository(db), new AttendanceRepository(db)),
        user, "Parent");

    [Fact]
    public async Task Children_ReturnsExactlyLinkedStudents_WithRelationshipAndActiveClass()
    {
        await using var db = Context();
        var (parent, childA, childB, unrelated) = await SeedFamilyAsync(db);
        var year = new SchoolYear { Name = "2569", StartDate = DateTime.Today.AddMonths(-1), EndDate = DateTime.Today.AddMonths(10) };
        db.SchoolYears.Add(year); await db.SaveChangesAsync();
        var schoolClass = new SchoolClass { Name = "ม.1/1", SchoolYearId = year.Id };
        db.SchoolClasses.Add(schoolClass); await db.SaveChangesAsync();
        db.ClassMemberships.Add(new ClassMembership { StudentId = childA.Id, ClassId = schoolClass.Id, JoinedDate = DateTime.Today.AddDays(-10) });
        await db.SaveChangesAsync();

        var result = await Guardians(db, parent).GetMyChildren();
        var children = ((OkObjectResult)result.Result!).Value.Should().BeAssignableTo<IEnumerable<GuardianChildDto>>().Subject.ToList();
        children.Select(x => x.Id).Should().BeEquivalentTo(new[] { childA.Id, childB.Id });
        children.Should().NotContain(x => x.Id == unrelated.Id);
        children.Single(x => x.Id == childA.Id).Should().Match<GuardianChildDto>(x => x.Relationship == "mother" && x.ActiveClassName == "ม.1/1" && x.Name.Nickname == "A");
    }

    [Fact]
    public async Task UnlinkedParent_GetsNotFoundForChildren()
    {
        await using var db = Context();
        var user = new User { Email = "unlinked-parent@test", PasswordHash = "x" };
        db.Users.Add(user); await db.SaveChangesAsync();
        (await Guardians(db, user).GetMyChildren()).Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Guardian_CanReadBothChildren_ButNotUnrelatedStudent()
    {
        await using var db = Context();
        var (parent, childA, childB, unrelated) = await SeedFamilyAsync(db);

        (await Schedule(db, parent, "Parent").GetStudentSchedule(childA.Id, null, null)).Result.Should().BeOfType<OkObjectResult>();
        (await Schedule(db, parent, "Parent").GetStudentSchedule(childB.Id, null, null)).Result.Should().BeOfType<OkObjectResult>();
        (await Schedule(db, parent, "Parent").GetStudentSchedule(unrelated.Id, null, null)).Result.Should().BeOfType<ForbidResult>();

        (await Attendance(db, parent, "Parent").GetStudentSummary(childA.Id, null, null)).Result.Should().BeOfType<OkObjectResult>();
        (await Attendance(db, parent, "Parent").GetStudentSummary(unrelated.Id, null, null)).Result.Should().BeOfType<ForbidResult>();
        (await Guardians(db, parent).GetChildAttendance(childB.Id, null, null)).Result.Should().BeOfType<OkObjectResult>();
        (await Guardians(db, parent).GetChildAttendance(unrelated.Id, null, null)).Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task ExistingAdminTeacherAndStudentReadRules_AreUnaffected()
    {
        await using var db = Context();
        var (_, child, _, unrelated) = await SeedFamilyAsync(db);
        var teacher = new Teacher { Name = new PersonName { FirstName = "Teacher", LastName = "One" }, Email = "teacher@test" };
        var teacherUser = new User { Email = "teacher-login@test", PasswordHash = "x", Teacher = teacher };
        var admin = new User { Email = "admin@test", PasswordHash = "x" };
        var studentUser = new User { Email = "student-login@test", PasswordHash = "x", StudentId = child.Id };
        db.AddRange(teacher, teacherUser, admin, studentUser); await db.SaveChangesAsync();

        (await Schedule(db, admin, "Admin").GetStudentSchedule(unrelated.Id, null, null)).Result.Should().BeOfType<OkObjectResult>();
        (await Schedule(db, teacherUser, "Teacher").GetStudentSchedule(unrelated.Id, null, null)).Result.Should().BeOfType<OkObjectResult>();
        (await Schedule(db, studentUser, "Student").GetStudentSchedule(child.Id, null, null)).Result.Should().BeOfType<OkObjectResult>();
        (await Schedule(db, studentUser, "Student").GetStudentSchedule(unrelated.Id, null, null)).Result.Should().BeOfType<ForbidResult>();
        (await Attendance(db, admin, "Admin").GetStudentSummary(unrelated.Id, null, null)).Result.Should().BeOfType<OkObjectResult>();
        (await Attendance(db, studentUser, "Student").GetStudentSummary(unrelated.Id, null, null)).Result.Should().BeOfType<ForbidResult>();
    }

    private static async Task<(User parent, Student childA, Student childB, Student unrelated)> SeedFamilyAsync(ApplicationDbContext db)
    {
        var childA = Student("A", "S1");
        var childB = Student("B", "S2");
        var unrelated = Student("X", "S3");
        var guardian = new Guardian { Name = new PersonName { FirstName = "Mali", LastName = "Parent" } };
        db.AddRange(childA, childB, unrelated, guardian); await db.SaveChangesAsync();
        db.StudentGuardians.AddRange(
            new StudentGuardian { StudentId = childA.Id, GuardianId = guardian.Id, Relationship = "mother" },
            new StudentGuardian { StudentId = childB.Id, GuardianId = guardian.Id, Relationship = "guardian" });
        var parent = new User { Email = "parent@test", PasswordHash = "x", GuardianId = guardian.Id };
        db.Users.Add(parent); await db.SaveChangesAsync();
        return (parent, childA, childB, unrelated);
    }

    private static Student Student(string nickname, string id) => new()
    {
        Name = new PersonName { FirstName = $"Child {nickname}", LastName = "Student", Nickname = nickname },
        Email = $"{id}@test", StudentId = id, IdCardNumber = $"ID-{id}", DateOfBirth = DateTime.Today.AddYears(-12),
        EnrollmentDate = DateTime.Today.AddYears(-1), PhoneNumber = "0", Address = "Bangkok"
    };
}
