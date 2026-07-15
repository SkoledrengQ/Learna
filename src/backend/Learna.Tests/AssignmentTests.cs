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

public class AssignmentTests
{
    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public void Model_EnforcesUniqueRowsAndFourWayFileTarget()
    {
        using var db = Context(); var model = db.GetService<IDesignTimeModel>().Model;
        model.FindEntityType(typeof(Submission))!.GetIndexes().Single(i => i.IsUnique).Properties.Select(p => p.Name).Should().Equal("AssignmentId", "StudentId");
        model.FindEntityType(typeof(AssignmentExtension))!.GetIndexes().Single(i => i.IsUnique).Properties.Select(p => p.Name).Should().Equal("AssignmentId", "StudentId");
        var sql = model.FindEntityType(typeof(FileResource))!.GetCheckConstraints().Single(c => c.Name == "CK_FileResource_ExactlyOneTarget").Sql;
        sql.Should().ContainAll("SubjectGroupId", "LessonId", "AssignmentId", "SubmissionId");
    }

    [Fact]
    public async Task Visibility_StartDate_AndThaiText_ArePreserved()
    {
        await using var db = Context(); var seed = await Seed(db); var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero)); var storage = new MemoryStorage();
        var teacher = Controller(db, seed.TeacherUser, clock, storage, "Teacher");
        var created = await teacher.Create(seed.Group.Id, new("การบ้านบทที่ 3", "คำอธิบายภาษาไทย", new DateOnly(2026, 7, 16), new DateOnly(2026, 7, 17), new TimeOnly(12, 0), LatePolicy.AllowMarkLate));
        var assignment = ((AssignmentDto)((CreatedAtActionResult)created.Result!).Value!);
        Values(await Controller(db, seed.StudentAUser, clock, storage, "Student").Mine()).Should().BeEmpty("draft assignments are invisible");
        await teacher.Publish(assignment.Id);
        Values(await Controller(db, seed.StudentAUser, clock, storage, "Student").Mine()).Should().BeEmpty("future StartDate is respected");
        clock.Set(new DateTimeOffset(2026, 7, 16, 0, 0, 0, TimeSpan.Zero));
        var visible = Values(await Controller(db, seed.StudentAUser, clock, storage, "Student").Mine()).Single();
        visible.Title.Should().Be("การบ้านบทที่ 3"); visible.Description.Should().Be("คำอธิบายภาษาไทย");
        (await Controller(db, seed.UnenrolledUser, clock, storage, "Student").Get(assignment.Id)).Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeadlineBoundary_Extension_Resubmission_AndClosedRules_AreEnforced()
    {
        await using var db = Context(); var seed = await Seed(db); var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero)); var storage = new MemoryStorage(); var teacher = Controller(db, seed.TeacherUser, clock, storage, "Teacher");
        var allow = await CreatePublished(teacher, seed.Group.Id, LatePolicy.AllowMarkLate, new DateOnly(2026, 7, 15), new TimeOnly(12, 0));
        var studentA = Controller(db, seed.StudentAUser, clock, storage, "Student");
        var first = Value(await studentA.Submit(allow.Id, [FormFile("งาน.pdf")], "คำตอบครั้งแรก", default));
        first.IsLate.Should().BeFalse("the exact deadline boundary is on time");
        clock.Set(new DateTimeOffset(2026, 7, 15, 12, 0, 1, TimeSpan.Zero));
        var second = Value(await studentA.Submit(allow.Id, [FormFile("แก้ไข.pdf")], "คำตอบแก้ไข", default));
        second.Id.Should().Be(first.Id); second.IsLate.Should().BeTrue(); second.Text.Should().Be("คำตอบแก้ไข");
        (await db.Submissions.CountAsync(s => s.AssignmentId == allow.Id && s.StudentId == seed.StudentA.Id)).Should().Be(1);

        await teacher.PutExtension(allow.Id, seed.StudentB.Id, new(new DateOnly(2026, 7, 22), new TimeOnly(12, 0), "ขยายเวลา"));
        var studentBResult = await Controller(db, seed.StudentBUser, clock, storage, "Student").Submit(allow.Id, [FormFile("b.pdf")], null, default);
        Value(studentBResult).IsLate.Should().BeFalse("only student B's effective deadline moved");
        second.IsLate.Should().BeTrue("student A keeps the original deadline");

        var block = await CreatePublished(teacher, seed.Group.Id, LatePolicy.Block, new DateOnly(2026, 7, 15), new TimeOnly(12, 0));
        (await studentA.Submit(block.Id, [FormFile("late.pdf")], null, default)).Result.Should().BeOfType<ConflictObjectResult>().Which.Value.Should().BeEquivalentTo(new AssignmentErrorDto("LATE_SUBMISSION_BLOCKED"));
        await teacher.Close(allow.Id);
        (await studentA.Submit(allow.Id, [FormFile("closed.pdf")], null, default)).Result.Should().BeOfType<ConflictObjectResult>().Which.Value.Should().BeEquivalentTo(new AssignmentErrorDto("ASSIGNMENT_CLOSED"));
    }

    [Fact]
    public async Task TeacherGuardianAndSubmissionFileAuthorization_IsScoped()
    {
        await using var db = Context(); var seed = await Seed(db); var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero)); var storage = new MemoryStorage();
        var teacher = Controller(db, seed.TeacherUser, clock, storage, "Teacher"); var a = await CreatePublished(teacher, seed.Group.Id, LatePolicy.AllowMarkLate, new DateOnly(2026, 7, 16), new TimeOnly(12, 0));
        await Controller(db, seed.StudentAUser, clock, storage, "Student").Submit(a.Id, [FormFile("private.pdf")], "ส่วนตัว", default);
        (await Controller(db, seed.OtherTeacherUser, clock, storage, "Teacher").Update(a.Id, new("x", null, null, a.DeadlineDate, a.DeadlineTime, a.LatePolicy))).Result.Should().BeOfType<ForbidResult>();
        (await Controller(db, seed.OtherTeacherUser, clock, storage, "Teacher").Submissions(a.Id)).Result.Should().BeOfType<ForbidResult>();
        Values(await Controller(db, seed.ParentUser, clock, storage, "Parent").GuardianAssignments(seed.StudentA.Id)).Should().ContainSingle();
        (await Controller(db, seed.OtherParentUser, clock, storage, "Parent").GuardianAssignments(seed.StudentA.Id)).Result.Should().BeOfType<ForbidResult>();
        var file = await db.FileResources.SingleAsync(f => f.SubmissionId != null);
        var filesController = Files(db, seed.StudentBUser, storage, "Student");
        (await filesController.Download(file.Id, default)).Should().BeOfType<ForbidResult>();
        (await Files(db, seed.TeacherUser, storage, "Teacher").Download(file.Id, default)).Should().BeOfType<FileStreamResult>();
        (await Files(db, seed.StudentAUser, storage, "Student").Delete(file.Id, default)).Should().BeOfType<ForbidResult>("submission files can only be replaced through resubmission rules");
    }

    private static async Task<AssignmentDto> CreatePublished(AssignmentsController controller, int groupId, LatePolicy policy, DateOnly date, TimeOnly time)
    {
        var created = await controller.Create(groupId, new("Homework", null, null, date, time, policy)); var dto = (AssignmentDto)((CreatedAtActionResult)created.Result!).Value!; await controller.Publish(dto.Id); return dto;
    }
    private static T Value<T>(ActionResult<T> result) => result.Value ?? (T)((ObjectResult)result.Result!).Value!;
    private static IEnumerable<T> Values<T>(ActionResult<IEnumerable<T>> result) => result.Value ?? (IEnumerable<T>)((ObjectResult)result.Result!).Value!;
    private static AssignmentsController Controller(ApplicationDbContext db, User user, TimeProvider clock, IFileStorage storage, params string[] roles)
    {
        var c = new AssignmentsController(new AssignmentRepository(db), new SubjectGroupRepository(db), new UserRepository(db), storage, clock, NullLogger<AssignmentsController>.Instance); SetUser(c, user, roles); return c;
    }
    private static FilesController Files(ApplicationDbContext db, User user, IFileStorage storage, params string[] roles) { var c = new FilesController(new FileResourceRepository(db), storage, new UserRepository(db), NullLogger<FilesController>.Instance); SetUser(c, user, roles); return c; }
    private static void SetUser(ControllerBase c, User user, IEnumerable<string> roles) { var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id.ToString()) }; claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r))); c.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) } }; }
    private static FormFile FormFile(string name) { var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "files", name) { Headers = new HeaderDictionary(), ContentType = "application/pdf" }; }

    private sealed record SeedData(User TeacherUser, User OtherTeacherUser, User StudentAUser, User StudentBUser, User UnenrolledUser, User ParentUser, User OtherParentUser, Student StudentA, Student StudentB, SubjectGroup Group);
    private static async Task<SeedData> Seed(ApplicationDbContext db)
    {
        var teacher = Teacher("t1"); var otherTeacher = Teacher("t2"); var a = Student("A"); var b = Student("B"); var outsider = Student("C"); var guardian = new Guardian { Name = new PersonName { FirstName = "P", LastName = "One" } }; var otherGuardian = new Guardian { Name = new PersonName { FirstName = "P", LastName = "Two" } }; var year = new SchoolYear { Name = "Y", StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) }; db.AddRange(teacher, otherTeacher, a, b, outsider, guardian, otherGuardian, year); await db.SaveChangesAsync();
        var term = new Term { Name = "T", SchoolYearId = year.Id, StartDate = year.StartDate, EndDate = year.EndDate }; var subject = new Subject { Code = "M", NameEnglish = "Math", NameThai = "คณิตศาสตร์" }; db.AddRange(term, subject); await db.SaveChangesAsync(); var group = new SubjectGroup { Name = "กลุ่มคณิตศาสตร์", SubjectId = subject.Id, TermId = term.Id, TeacherId = teacher.Id }; db.Add(group); await db.SaveChangesAsync(); db.AddRange(new Enrollment { StudentId = a.Id, SubjectGroupId = group.Id, EnrolledDate = DateTime.UtcNow }, new Enrollment { StudentId = b.Id, SubjectGroupId = group.Id, EnrolledDate = DateTime.UtcNow }, new StudentGuardian { StudentId = a.Id, GuardianId = guardian.Id, Relationship = "parent" });
        var users = new[] { User("teacher", teacherId: teacher.Id), User("other-teacher", teacherId: otherTeacher.Id), User("student-a", studentId: a.Id), User("student-b", studentId: b.Id), User("outsider", studentId: outsider.Id), User("parent", guardianId: guardian.Id), User("other-parent", guardianId: otherGuardian.Id) }; db.AddRange(users); await db.SaveChangesAsync(); return new(users[0], users[1], users[2], users[3], users[4], users[5], users[6], a, b, group);
    }
    private static Teacher Teacher(string id) => new() { Name = new PersonName { FirstName = id, LastName = "Teacher" }, Email = $"{id}@test" };
    private static Student Student(string id) => new() { Name = new PersonName { FirstName = id, LastName = "Student" }, Email = $"{id}@test", StudentId = id, IdCardNumber = $"ID{id}", DateOfBirth = DateTime.Today, EnrollmentDate = DateTime.Today, PhoneNumber = "0", Address = "x" };
    private static User User(string id, int? teacherId = null, int? studentId = null, int? guardianId = null) => new() { Email = $"{id}@test", PasswordHash = "x", TeacherId = teacherId, StudentId = studentId, GuardianId = guardianId };

    private sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider { private DateTimeOffset value = now; public override DateTimeOffset GetUtcNow() => value; public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc; public void Set(DateTimeOffset next) => value = next; }
    private sealed class MemoryStorage : IFileStorage
    {
        private readonly HashSet<string> names = []; public Task<StoredFile> StoreAsync(Stream source, string originalFileName, long sizeBytes, CancellationToken cancellationToken = default) { var name = $"{Guid.NewGuid():N}.pdf"; names.Add(name); return Task.FromResult(new StoredFile("test", name, sizeBytes)); }
        public Task<Stream?> OpenReadAsync(string storedPath, string storedName, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(names.Contains(storedName) ? new MemoryStream([1]) : null);
        public Task DeleteAsync(string storedPath, string storedName, CancellationToken cancellationToken = default) { names.Remove(storedName); return Task.CompletedTask; }
    }
}
