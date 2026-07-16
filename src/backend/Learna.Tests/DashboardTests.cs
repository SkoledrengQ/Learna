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

namespace Learna.Tests;

public class DashboardTests
{
    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static DashboardController Controller(ApplicationDbContext db, User user, string role, TimeProvider? clock = null)
    {
        var controller = new DashboardController(new UserRepository(db), new LessonRepository(db), new AssignmentRepository(db),
            new StudentRepository(db), new TeacherRepository(db), new SchoolClassRepository(db), new SubjectGroupRepository(db),
            clock ?? new TestTimeProvider(new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero)));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, role)
                }, "test"))
            }
        };
        return controller;
    }

    private static Teacher Teacher(string id) => new() { Name = new PersonName { FirstName = id, LastName = "Teacher" }, Email = $"{id}@test" };
    private static Student Student(string id) => new() { Name = new PersonName { FirstName = id, LastName = "Student" }, Email = $"{id}@test", StudentId = id, IdCardNumber = $"ID{id}", DateOfBirth = DateTime.Today, EnrollmentDate = DateTime.Today, PhoneNumber = "0", Address = "x" };
    private static User User(string id, int? teacherId = null, int? studentId = null) => new() { Email = $"{id}@test", PasswordHash = "x", TeacherId = teacherId, StudentId = studentId };

    private static async Task<(SchoolYear year, Term term, Subject subject)> RefDataAsync(ApplicationDbContext db)
    {
        var year = new SchoolYear { Name = "Y", StartDate = DateTime.Today.AddMonths(-6), EndDate = DateTime.Today.AddMonths(6) };
        db.Add(year); await db.SaveChangesAsync();
        var term = new Term { SchoolYearId = year.Id, Name = "T", StartDate = year.StartDate, EndDate = year.EndDate };
        var subject = new Subject { Code = "M", NameEnglish = "Math", NameThai = "คณิตศาสตร์" };
        db.AddRange(term, subject); await db.SaveChangesAsync();
        return (year, term, subject);
    }

    [Fact]
    public async Task TeacherDashboard_ScopesToOwnLessonsAndGroups_AndForbidsNonTeachers()
    {
        await using var db = Context();
        var (_, term, subject) = await RefDataAsync(db);
        var teacherA = Teacher("A"); var teacherB = Teacher("B"); var studentOnlyUser = Student("Outsider");
        db.AddRange(teacherA, teacherB, studentOnlyUser); await db.SaveChangesAsync();
        var groupA = new SubjectGroup { Name = "Group A", SubjectId = subject.Id, TermId = term.Id, TeacherId = teacherA.Id };
        var groupB = new SubjectGroup { Name = "Group B", SubjectId = subject.Id, TermId = term.Id, TeacherId = teacherB.Id };
        db.AddRange(groupA, groupB); await db.SaveChangesAsync();

        var today = new DateOnly(2026, 7, 16);
        var lessonA = new Lesson { SubjectGroupId = groupA.Id, TeacherId = teacherA.Id, Date = today, StartTime = new(8, 0), EndTime = new(9, 0) };
        var lessonB = new Lesson { SubjectGroupId = groupB.Id, TeacherId = teacherB.Id, Date = today, StartTime = new(8, 0), EndTime = new(9, 0) };
        db.AddRange(lessonA, lessonB); await db.SaveChangesAsync();

        var teacherAUser = User("teacher-a", teacherId: teacherA.Id);
        var outsiderUser = User("outsider", studentId: studentOnlyUser.Id);
        db.AddRange(teacherAUser, outsiderUser); await db.SaveChangesAsync();

        var result = await Controller(db, teacherAUser, "Teacher").Teacher();
        var dto = ((OkObjectResult)result.Result!).Value as TeacherDashboardDto;
        dto!.TodayLessons.Should().ContainSingle().Which.SubjectGroupId.Should().Be(groupA.Id);
        dto.MissingAttendanceLessons.Should().ContainSingle().Which.SubjectGroupId.Should().Be(groupA.Id, "teacher A must not see teacher B's missing-attendance lesson");

        (await Controller(db, outsiderUser, "Student").Teacher()).Result.Should().BeOfType<ForbidResult>("a caller with no TeacherId must be forbidden even if the action were reached");

        typeof(DashboardController).GetMethod(nameof(DashboardController.Teacher))!
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Single().Roles.Should().Be("Teacher");
    }

    [Fact]
    public async Task MissingAttendance_ExcludesCancelled_Future_AndOutOfWindow_CountsPastAndTodayOnly()
    {
        await using var db = Context();
        var (_, term, subject) = await RefDataAsync(db);
        var teacher = Teacher("T"); db.Add(teacher); await db.SaveChangesAsync();
        var group = new SubjectGroup { Name = "Group", SubjectId = subject.Id, TermId = term.Id, TeacherId = teacher.Id };
        db.Add(group); await db.SaveChangesAsync();
        var teacherUser = User("teacher", teacherId: teacher.Id); db.Add(teacherUser); await db.SaveChangesAsync();

        var today = new DateOnly(2026, 7, 16);
        var missingToday = new Lesson { SubjectGroupId = group.Id, TeacherId = teacher.Id, Date = today, StartTime = new(8, 0), EndTime = new(9, 0) };
        var cancelledToday = new Lesson { SubjectGroupId = group.Id, TeacherId = teacher.Id, Date = today, StartTime = new(9, 0), EndTime = new(10, 0), Status = LessonStatus.Cancelled };
        var recordedYesterday = new Lesson { SubjectGroupId = group.Id, TeacherId = teacher.Id, Date = today.AddDays(-1), StartTime = new(8, 0), EndTime = new(9, 0) };
        var outOfWindow = new Lesson { SubjectGroupId = group.Id, TeacherId = teacher.Id, Date = today.AddDays(-10), StartTime = new(8, 0), EndTime = new(9, 0) };
        var future = new Lesson { SubjectGroupId = group.Id, TeacherId = teacher.Id, Date = today.AddDays(1), StartTime = new(8, 0), EndTime = new(9, 0) };
        db.AddRange(missingToday, cancelledToday, recordedYesterday, outOfWindow, future); await db.SaveChangesAsync();

        var student = Student("S"); db.Add(student); await db.SaveChangesAsync();
        db.Add(new Enrollment { StudentId = student.Id, SubjectGroupId = group.Id, EnrolledDate = DateTime.Today.AddYears(-1) });
        db.Add(new AttendanceRecord { LessonId = recordedYesterday.Id, StudentId = student.Id, Status = AttendanceStatus.Present, RecordedByUserId = teacherUser.Id, RecordedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero));
        var result = await Controller(db, teacherUser, "Teacher", clock).Teacher();
        var dto = ((OkObjectResult)result.Result!).Value as TeacherDashboardDto;

        dto!.MissingAttendanceCount.Should().Be(1);
        dto.MissingAttendanceLessons.Should().ContainSingle().Which.Id.Should().Be(missingToday.Id);
    }

    [Fact]
    public async Task AwaitingGrading_CountsOnlyUngradedSubmissions_AcrossTeachersGroups()
    {
        await using var db = Context();
        var (_, term, subject) = await RefDataAsync(db);
        var teacher = Teacher("T"); db.Add(teacher); await db.SaveChangesAsync();
        var group = new SubjectGroup { Name = "Group", SubjectId = subject.Id, TermId = term.Id, TeacherId = teacher.Id };
        db.Add(group); await db.SaveChangesAsync();
        var studentA = Student("A"); var studentB = Student("B"); db.AddRange(studentA, studentB); await db.SaveChangesAsync();
        var teacherUser = User("teacher", teacherId: teacher.Id); db.Add(teacherUser); await db.SaveChangesAsync();

        var assignment = new Assignment { SubjectGroupId = group.Id, Title = "HW1", DeadlineDate = new DateOnly(2026, 7, 20), DeadlineTime = new TimeOnly(12, 0), CreatedByUserId = teacherUser.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Add(assignment); await db.SaveChangesAsync();
        var ungraded = new Submission { AssignmentId = assignment.Id, StudentId = studentA.Id, SubmittedAt = new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc) };
        var graded = new Submission { AssignmentId = assignment.Id, StudentId = studentB.Id, SubmittedAt = new DateTime(2026, 7, 15, 11, 0, 0, DateTimeKind.Utc) };
        db.AddRange(ungraded, graded); await db.SaveChangesAsync();
        db.Add(new Grade { StudentId = studentB.Id, SubjectGroupId = group.Id, SubmissionId = graded.Id, Category = "HW1", Score = 8, MaxScore = 10, GradedByUserId = teacherUser.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero));
        var result = await Controller(db, teacherUser, "Teacher", clock).Teacher();
        var dto = ((OkObjectResult)result.Result!).Value as TeacherDashboardDto;

        dto!.AwaitingGradingCount.Should().Be(1);
        dto.RecentSubmissions.Should().ContainSingle().Which.SubmissionId.Should().Be(ungraded.Id);
    }

    [Fact]
    public async Task AdminDashboard_CountsMatchFixture_AndAreSchoolWide_ForbidsNonAdmins()
    {
        await using var db = Context();
        var (year, term, subject) = await RefDataAsync(db);
        var teacherA = Teacher("A"); var teacherB = Teacher("B");
        db.AddRange(teacherA, teacherB); await db.SaveChangesAsync();
        var groupA = new SubjectGroup { Name = "Group A", SubjectId = subject.Id, TermId = term.Id, TeacherId = teacherA.Id };
        var groupB = new SubjectGroup { Name = "Group B", SubjectId = subject.Id, TermId = term.Id, TeacherId = teacherB.Id };
        db.AddRange(groupA, groupB); await db.SaveChangesAsync();
        var schoolClass = new SchoolClass { Name = "ม.1/1", SchoolYearId = year.Id };
        db.Add(schoolClass); await db.SaveChangesAsync();
        var students = new[] { Student("S1"), Student("S2"), Student("S3") };
        db.AddRange(students); await db.SaveChangesAsync();

        var today = new DateOnly(2026, 7, 16);
        var missingA = new Lesson { SubjectGroupId = groupA.Id, TeacherId = teacherA.Id, Date = today, StartTime = new(8, 0), EndTime = new(9, 0) };
        var missingB = new Lesson { SubjectGroupId = groupB.Id, TeacherId = teacherB.Id, Date = today, StartTime = new(9, 0), EndTime = new(10, 0) };
        var otherToday = new Lesson { SubjectGroupId = groupA.Id, TeacherId = teacherA.Id, Date = today, StartTime = new(10, 0), EndTime = new(11, 0) };
        db.AddRange(missingA, missingB, otherToday); await db.SaveChangesAsync();
        db.Add(new Enrollment { StudentId = students[0].Id, SubjectGroupId = groupA.Id, EnrolledDate = DateTime.Today.AddYears(-1) });
        db.Add(new AttendanceRecord { LessonId = otherToday.Id, StudentId = students[0].Id, Status = AttendanceStatus.Present, RecordedByUserId = 1, RecordedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var adminUser = User("admin"); db.Add(adminUser); await db.SaveChangesAsync();
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero));
        var result = await Controller(db, adminUser, "Admin", clock).Admin();
        var dto = ((OkObjectResult)result.Result!).Value as AdminDashboardDto;

        dto!.StudentCount.Should().Be(3);
        dto.TeacherCount.Should().Be(2);
        dto.ClassCount.Should().Be(1);
        dto.SubjectGroupCount.Should().Be(2);
        dto.TodayLessonCount.Should().Be(3);
        dto.MissingAttendanceCount.Should().Be(2, "school-wide count must include both teachers' missing-attendance lessons");

        typeof(DashboardController).GetMethod(nameof(DashboardController.Admin))!
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Single().Roles.Should().Be("Admin");
    }

    private sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
