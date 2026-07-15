using System.Security.Claims;
using FluentAssertions;
using Learna.Api.Controllers;
using Learna.Api.DTOs;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;
using Learna.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;

namespace Learna.Tests;

public class GradeTests
{
    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public void Model_HasOneGradePerSubmission_AndScoreConstraints()
    {
        using var db = Context(); var entity = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Grade))!;
        entity.GetIndexes().Single(i => i.IsUnique).Properties.Select(p => p.Name).Should().Equal("SubmissionId");
        entity.GetCheckConstraints().Select(c => c.Name).Should().Contain(["CK_Grade_MaxScore_Positive", "CK_Grade_Score_Bounds"]);
    }

    [Fact]
    public async Task DraftInvisible_PublishedVisible_ThaiRoundTrips_AndGuardianIsScoped()
    {
        await using var db = Context(); var s = await Seed(db); var teacher = Grades(db, s.TeacherUser, "Teacher");
        var created = Value(await teacher.GradeSubmission(s.SubmissionA.Id, new(8, 10, "ทำได้ดีมาก", "การบ้าน")));
        created.Status.Should().Be(GradeStatus.Draft); created.Category.Should().Be("การบ้าน"); created.Feedback.Should().Be("ทำได้ดีมาก");
        Values(await Grades(db, s.StudentAUser, "Student").Mine()).Should().BeEmpty();
        Values(await Grades(db, s.ParentUser, "Parent").GuardianGrades(s.StudentA.Id)).Should().BeEmpty();
        await teacher.PublishOne(created.Id);
        var own = Values(await Grades(db, s.StudentAUser, "Student").Mine()).Single();
        own.AveragePercentage.Should().Be(80); own.Grades.Single().Feedback.Should().Be("ทำได้ดีมาก");
        Values(await Grades(db, s.ParentUser, "Parent").GuardianGrades(s.StudentA.Id)).Single().Grades.Should().ContainSingle();
        (await Grades(db, s.OtherParentUser, "Parent").GuardianGrades(s.StudentA.Id)).Result.Should().BeOfType<ForbidResult>();
        Values(await Grades(db, s.StudentBUser, "Student").Mine()).Should().BeEmpty("students never see another student's grade");
    }

    [Fact]
    public async Task PublishLocksOnlyThatSubmission_AndLastPublishedGradeMarksAssignmentGraded()
    {
        await using var db = Context(); var s = await Seed(db); var teacher = Grades(db, s.TeacherUser, "Teacher");
        var gradeA = Value(await teacher.GradeSubmission(s.SubmissionA.Id, new(8, 10, null, null)));
        var gradeB = Value(await teacher.GradeSubmission(s.SubmissionB.Id, new(9, 10, null, null)));
        await teacher.PublishOne(gradeA.Id);
        var assignmentController = Assignments(db, s.StudentAUser, "Student");
        (await assignmentController.Submit(s.Assignment.Id, null, "revision", default)).Result.Should().BeOfType<ConflictObjectResult>().Which.Value.Should().BeEquivalentTo(new AssignmentErrorDto("PUBLISHED_GRADE_LOCKS_SUBMISSION"));
        Value(await Assignments(db, s.StudentBUser, "Student").Submit(s.Assignment.Id, null, "revision B", default)).Text.Should().Be("revision B");
        (await db.Assignments.FindAsync(s.Assignment.Id))!.Status.Should().Be(AssignmentStatus.Published);
        await teacher.PublishOne(gradeB.Id);
        (await db.Assignments.FindAsync(s.Assignment.Id))!.Status.Should().Be(AssignmentStatus.Graded);
    }

    [Fact]
    public async Task BoundsRosterManualGradesDeleteAndTeacherAuthorizationAreEnforced()
    {
        await using var db = Context(); var s = await Seed(db); var teacher = Grades(db, s.TeacherUser, "Teacher");
        (await teacher.GradeSubmission(s.SubmissionA.Id, new(11, 10))).Result.Should().BeOfType<BadRequestObjectResult>();
        (await teacher.CreateManual(s.Group.Id, new(s.Outsider.Id, "Quiz", 5, 10))).Result.Should().BeOfType<BadRequestObjectResult>();
        (await Grades(db, s.OtherTeacherUser, "Teacher").GroupGrades(s.Group.Id)).Result.Should().BeOfType<ForbidResult>();
        var manual = Value(await teacher.CreateManual(s.Group.Id, new(s.StudentA.Id, "Quiz 1", 7, 10, "ดี")));
        Values(await teacher.GroupGrades(s.Group.Id)).Should().Contain(g => g.SubmissionId == null && g.Category == "Quiz 1");
        await teacher.PublishOne(manual.Id);
        Values(await Grades(db, s.StudentAUser, "Student").Mine()).Single().Grades.Should().Contain(g => g.Category == "Quiz 1");
        (await teacher.Delete(manual.Id)).Should().BeOfType<ConflictObjectResult>();
    }

    private static GradesController Grades(ApplicationDbContext db, User user, params string[] roles) { var c = new GradesController(new GradeRepository(db), new UserRepository(db)); SetUser(c, user, roles); return c; }
    private static AssignmentsController Assignments(ApplicationDbContext db, User user, params string[] roles) { var c = new AssignmentsController(new AssignmentRepository(db), new SubjectGroupRepository(db), new UserRepository(db), new MemoryStorage(), TimeProvider.System, NullLogger<AssignmentsController>.Instance); SetUser(c, user, roles); return c; }
    private static void SetUser(ControllerBase c, User user, IEnumerable<string> roles) { var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id.ToString()) }; claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r))); c.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) } }; }
    private static T Value<T>(ActionResult<T> result) => result.Value ?? (T)((ObjectResult)result.Result!).Value!;
    private static IEnumerable<T> Values<T>(ActionResult<IEnumerable<T>> result) => result.Value ?? (IEnumerable<T>)((ObjectResult)result.Result!).Value!;

    private sealed record SeedData(User TeacherUser, User OtherTeacherUser, User StudentAUser, User StudentBUser, User ParentUser, User OtherParentUser, Student StudentA, Student StudentB, Student Outsider, SubjectGroup Group, Assignment Assignment, Submission SubmissionA, Submission SubmissionB);
    private static async Task<SeedData> Seed(ApplicationDbContext db)
    {
        var teacher = Teacher("t1"); var otherTeacher = Teacher("t2"); var a = Student("A"); var b = Student("B"); var outsider = Student("C"); var parent = new Guardian { Name = new PersonName { FirstName = "P", LastName = "One" } }; var otherParent = new Guardian { Name = new PersonName { FirstName = "P", LastName = "Two" } }; var year = new SchoolYear { Name = "Y", StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) }; db.AddRange(teacher, otherTeacher, a, b, outsider, parent, otherParent, year); await db.SaveChangesAsync();
        var term = new Term { Name = "T", SchoolYearId = year.Id, StartDate = year.StartDate, EndDate = year.EndDate }; var subject = new Subject { Code = "M", NameEnglish = "Math", NameThai = "คณิตศาสตร์" }; db.AddRange(term, subject); await db.SaveChangesAsync();
        var group = new SubjectGroup { Name = "กลุ่มคณิตศาสตร์", SubjectId = subject.Id, TermId = term.Id, TeacherId = teacher.Id }; db.Add(group); await db.SaveChangesAsync(); db.AddRange(new Enrollment { StudentId = a.Id, SubjectGroupId = group.Id, EnrolledDate = DateTime.UtcNow }, new Enrollment { StudentId = b.Id, SubjectGroupId = group.Id, EnrolledDate = DateTime.UtcNow }, new StudentGuardian { StudentId = a.Id, GuardianId = parent.Id, Relationship = "parent" });
        var users = new[] { User("teacher", teacherId: teacher.Id), User("other-teacher", teacherId: otherTeacher.Id), User("student-a", studentId: a.Id), User("student-b", studentId: b.Id), User("parent", guardianId: parent.Id), User("other-parent", guardianId: otherParent.Id) }; db.AddRange(users); await db.SaveChangesAsync();
        var assignment = new Assignment { SubjectGroupId = group.Id, Title = "การบ้านบทที่ 3", DeadlineDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), DeadlineTime = new TimeOnly(12, 0), LatePolicy = LatePolicy.AllowMarkLate, Status = AssignmentStatus.Published, CreatedByUserId = users[0].Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }; db.Add(assignment); await db.SaveChangesAsync(); var sa = new Submission { AssignmentId = assignment.Id, StudentId = a.Id, Text = "A", SubmittedAt = DateTime.UtcNow }; var sb = new Submission { AssignmentId = assignment.Id, StudentId = b.Id, Text = "B", SubmittedAt = DateTime.UtcNow }; db.AddRange(sa, sb); await db.SaveChangesAsync(); return new(users[0], users[1], users[2], users[3], users[4], users[5], a, b, outsider, group, assignment, sa, sb);
    }
    private static Teacher Teacher(string id) => new() { Name = new PersonName { FirstName = id, LastName = "Teacher" }, Email = $"{id}@test" };
    private static Student Student(string id) => new() { Name = new PersonName { FirstName = id, LastName = "Student" }, Email = $"{id}@test", StudentId = id, IdCardNumber = $"ID{id}", DateOfBirth = DateTime.Today, EnrollmentDate = DateTime.Today, PhoneNumber = "0", Address = "x" };
    private static User User(string id, int? teacherId = null, int? studentId = null, int? guardianId = null) => new() { Email = $"{id}@test", PasswordHash = "x", TeacherId = teacherId, StudentId = studentId, GuardianId = guardianId };
    private sealed class MemoryStorage : IFileStorage { public Task<StoredFile> StoreAsync(Stream source, string originalFileName, long sizeBytes, CancellationToken cancellationToken = default) => Task.FromResult(new StoredFile("x", "x", sizeBytes)); public Task<Stream?> OpenReadAsync(string storedPath, string storedName, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null); public Task DeleteAsync(string storedPath, string storedName, CancellationToken cancellationToken = default) => Task.CompletedTask; }
}
