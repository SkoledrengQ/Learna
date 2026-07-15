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

public class AttendanceTests
{
    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static IConfiguration Config(int days = 7) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Attendance:EditWindowDays"] = days.ToString() }).Build();

    private static AttendanceController Controller(ApplicationDbContext db, User user, string role, int days = 7)
    {
        var controller = new AttendanceController(new LessonRepository(db), new AttendanceRepository(db), new UserRepository(db), new StudentRepository(db), Config(days));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Role, role) }, "test")) } };
        return controller;
    }

    private static async Task<(Teacher teacher, User teacherUser, User admin, SubjectGroup group, Student student)> SeedAsync(ApplicationDbContext db)
    {
        var teacher = new Teacher { Name = new PersonName { FirstName = "Wipa", LastName = "Jaidee" }, Email = "teacher@test" };
        var student = new Student { Name = new PersonName { FirstName = "Somchai", LastName = "Srisuk", Nickname = "Chai" }, Email = "student@test", StudentId = "S1", IdCardNumber = "ID1", DateOfBirth = DateTime.Today.AddYears(-12), EnrollmentDate = DateTime.Today.AddYears(-1), PhoneNumber = "0", Address = "Bangkok" };
        var year = new SchoolYear { Name = "Year", StartDate = DateTime.Today.AddYears(-1), EndDate = DateTime.Today.AddYears(1) };
        db.AddRange(teacher, student, year); await db.SaveChangesAsync();
        var term = new Term { SchoolYearId = year.Id, Name = "Term", StartDate = year.StartDate, EndDate = year.EndDate };
        var subject = new Subject { Code = "MATH", NameEnglish = "Mathematics", NameThai = "คณิตศาสตร์" };
        db.AddRange(term, subject); await db.SaveChangesAsync();
        var group = new SubjectGroup { SubjectId = subject.Id, TermId = term.Id, Name = "Math Group", TeacherId = teacher.Id };
        var teacherUser = new User { Email = "teacher-user@test", PasswordHash = "x", TeacherId = teacher.Id };
        var admin = new User { Email = "admin@test", PasswordHash = "x" };
        db.AddRange(group, teacherUser, admin); await db.SaveChangesAsync();
        return (teacher, teacherUser, admin, group, student);
    }

    [Fact]
    public void AttendanceRecord_HasUniqueLessonStudentIndex()
    {
        using var db = Context();
        var index = db.Model.FindEntityType(typeof(AttendanceRecord))!.GetIndexes().Single(i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { "LessonId", "StudentId" }));
        index.IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task Roster_RespectsEnrollmentDateWindow()
    {
        await using var db = Context(); var x = await SeedAsync(db); var date = DateOnly.FromDateTime(DateTime.Today);
        var before = new Student { Name = new PersonName { FirstName = "Before", LastName = "Student" }, Email = "before@test", StudentId = "S2", IdCardNumber = "ID2", DateOfBirth = DateTime.Today, EnrollmentDate = DateTime.Today, PhoneNumber = "0", Address = "x" };
        var after = new Student { Name = new PersonName { FirstName = "After", LastName = "Student" }, Email = "after@test", StudentId = "S3", IdCardNumber = "ID3", DateOfBirth = DateTime.Today, EnrollmentDate = DateTime.Today, PhoneNumber = "0", Address = "x" };
        db.AddRange(before, after); await db.SaveChangesAsync();
        db.Enrollments.AddRange(new Enrollment { StudentId = x.student.Id, SubjectGroupId = x.group.Id, EnrolledDate = DateTime.Today.AddDays(-2), UnenrolledDate = DateTime.Today }, new Enrollment { StudentId = before.Id, SubjectGroupId = x.group.Id, EnrolledDate = DateTime.Today.AddDays(-2), UnenrolledDate = DateTime.Today.AddDays(-1) }, new Enrollment { StudentId = after.Id, SubjectGroupId = x.group.Id, EnrolledDate = DateTime.Today.AddDays(1) }); await db.SaveChangesAsync();
        var roster = await new AttendanceRepository(db).GetRosterAsync(x.group.Id, date);
        roster.Select(s => s.Id).Should().Equal(x.student.Id);
    }

    [Fact]
    public async Task Teacher_CanWriteOwnLesson_ButNotAnotherTeachers_AndThaiNoteRoundTrips()
    {
        await using var db = Context(); var x = await SeedAsync(db); var date = DateOnly.FromDateTime(DateTime.Today);
        db.Enrollments.Add(new Enrollment { StudentId = x.student.Id, SubjectGroupId = x.group.Id, EnrolledDate = DateTime.Today.AddDays(-1) });
        var own = new Lesson { SubjectGroupId = x.group.Id, TeacherId = x.teacher.Id, Date = date, StartTime = new(8, 0), EndTime = new(9, 0) };
        var other = new Lesson { SubjectGroupId = x.group.Id, TeacherId = x.teacher.Id + 99, Date = date, StartTime = new(10, 0), EndTime = new(11, 0) };
        db.AddRange(own, other); await db.SaveChangesAsync(); var controller = Controller(db, x.teacherUser, "Teacher");
        var saved = await controller.PutLesson(own.Id, new BulkAttendanceDto(new[] { new AttendanceUpsertItemDto(x.student.Id, AttendanceStatus.Late, "มาสายเพราะฝนตก") }));
        saved.Result.Should().BeOfType<OkObjectResult>(); (await db.AttendanceRecords.SingleAsync()).Note.Should().Be("มาสายเพราะฝนตก");
        (await controller.PutLesson(other.Id, new BulkAttendanceDto(Array.Empty<AttendanceUpsertItemDto>()))).Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task WindowCancelledAndOffRosterRules_AreEnforced_AdminBypassesWindow()
    {
        await using var db = Context(); var x = await SeedAsync(db); var oldDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-8));
        db.Enrollments.Add(new Enrollment { StudentId = x.student.Id, SubjectGroupId = x.group.Id, EnrolledDate = DateTime.Today.AddYears(-1) });
        var old = new Lesson { SubjectGroupId = x.group.Id, TeacherId = x.teacher.Id, Date = oldDate, StartTime = new(8, 0), EndTime = new(9, 0) };
        var cancelled = new Lesson { SubjectGroupId = x.group.Id, TeacherId = x.teacher.Id, Date = DateOnly.FromDateTime(DateTime.Today), StartTime = new(10, 0), EndTime = new(11, 0), Status = LessonStatus.Cancelled };
        db.AddRange(old, cancelled); await db.SaveChangesAsync(); var dto = new BulkAttendanceDto(new[] { new AttendanceUpsertItemDto(x.student.Id, AttendanceStatus.Present, null) });
        (await Controller(db, x.teacherUser, "Teacher").PutLesson(old.Id, dto)).Result.Should().BeOfType<BadRequestObjectResult>();
        (await Controller(db, x.admin, "Admin").PutLesson(old.Id, dto)).Result.Should().BeOfType<OkObjectResult>();
        (await Controller(db, x.admin, "Admin").PutLesson(cancelled.Id, dto)).Result.Should().BeOfType<BadRequestObjectResult>();
        (await Controller(db, x.admin, "Admin").PutLesson(old.Id, new BulkAttendanceDto(new[] { new AttendanceUpsertItemDto(9999, AttendanceStatus.Present, null) }))).Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SummaryMath_CountsLatePresent_AndSplitsExcusedFromUnexcused()
    {
        await using var db = Context(); var x = await SeedAsync(db); var statuses = new[] { AttendanceStatus.Present, AttendanceStatus.Late, AttendanceStatus.Absent, AttendanceStatus.ExcusedAbsence, AttendanceStatus.Sick, AttendanceStatus.ApprovedLeave }; var today = DateOnly.FromDateTime(DateTime.Today);
        for (var i = 0; i < statuses.Length; i++) { var lesson = new Lesson { SubjectGroupId = x.group.Id, TeacherId = x.teacher.Id, Date = today.AddDays(-i), StartTime = new(8, 0), EndTime = new(9, 0) }; db.Lessons.Add(lesson); await db.SaveChangesAsync(); db.AttendanceRecords.Add(new AttendanceRecord { LessonId = lesson.Id, StudentId = x.student.Id, Status = statuses[i], RecordedByUserId = x.admin.Id, RecordedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }); }
        await db.SaveChangesAsync(); var result = await Controller(db, x.admin, "Admin").GetStudentSummary(x.student.Id, null, null); var summary = ((OkObjectResult)result.Result!).Value.Should().BeOfType<AttendanceSummaryDto>().Subject;
        summary.RecordedLessons.Should().Be(6); summary.PresencePercentage.Should().Be(33.33m); summary.ExcusedAbsences.Should().Be(3); summary.UnexcusedAbsences.Should().Be(1); summary.Totals.Late.Should().Be(1);
    }
}
