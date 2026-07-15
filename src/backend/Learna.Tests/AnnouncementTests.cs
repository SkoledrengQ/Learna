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

public class AnnouncementTests
{
    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public void Model_EnforcesReadUniquenessAndFifthFileTarget()
    {
        using var db = Context(); var model = db.GetService<IDesignTimeModel>().Model;
        model.FindEntityType(typeof(AnnouncementRead))!.GetIndexes().Single(i => i.IsUnique).Properties.Select(p => p.Name).Should().Equal("AnnouncementId", "UserId");
        model.FindEntityType(typeof(FileResource))!.GetCheckConstraints().Single(c => c.Name == "CK_FileResource_ExactlyOneTarget").Sql.Should().Contain("AnnouncementId");
    }

    [Fact]
    public async Task Feed_RelevancePerRole_AndDetailForbid_AreEnforced()
    {
        await using var db = Context(); var seed = await Seed(db); var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));
        var admin = Controller(db, seed.AdminUser, clock, "Admin");
        var school = Value(await admin.Create(new("School note", "All", AnnouncementAudienceType.School, null, null, null)));
        var classA = Value(await admin.Create(new("Class A", "Room", AnnouncementAudienceType.SchoolClass, seed.ClassA.Id, null, null)));
        var groupA = Value(await admin.Create(new("Group A", "Math", AnnouncementAudienceType.SubjectGroup, seed.GroupA.Id, null, null)));
        var groupB = Value(await admin.Create(new("Group B", "Other", AnnouncementAudienceType.SubjectGroup, seed.GroupB.Id, null, null)));

        Values(await Controller(db, seed.StudentAUser, clock, "Student").My()).Select(a => a.Title).Should().BeEquivalentTo(["Group A", "Class A", "School note"]);
        Values(await Controller(db, seed.StudentBUser, clock, "Student").My()).Select(a => a.Title).Should().Contain("Group B").And.NotContain("Group A");
        Values(await Controller(db, seed.ParentUser, clock, "Parent").My()).Select(a => a.Title).Should().Contain(["Group A", "Class A"]).And.NotContain("Group B");
        Values(await Controller(db, seed.TeacherUser, clock, "Teacher").My()).Select(a => a.Title).Should().Contain(["Group A", "Class A"]).And.NotContain("Group B");
        (await Controller(db, seed.StudentBUser, clock, "Student").Get(groupA.Id)).Result.Should().BeOfType<ForbidResult>();
        Values(await admin.My()).Select(a => a.Id).Should().Contain([school.Id, classA.Id, groupA.Id, groupB.Id]);
    }

    [Fact]
    public async Task Authoring_FutureExpiredReadAndThaiText_AreHandled()
    {
        await using var db = Context(); var seed = await Seed(db); var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero));
        var teacher = Controller(db, seed.TeacherUser, clock, "Teacher");
        var thai = Value(await teacher.Create(new("ประกาศปิดเรียน", "เนื้อหา <script>alert(1)</script>", AnnouncementAudienceType.SubjectGroup, seed.GroupA.Id, null, null)));
        thai.Title.Should().Be("ประกาศปิดเรียน");
        thai.Body.Should().Contain("<script>alert(1)</script>");
        (await teacher.Create(new("Bad class", "No", AnnouncementAudienceType.SchoolClass, seed.ClassB.Id, null, null))).Result.Should().BeOfType<ForbidResult>();

        var futureAt = clock.GetUtcNow().UtcDateTime.AddDays(1);
        var future = Value(await teacher.Create(new("Tomorrow", "Later", AnnouncementAudienceType.SubjectGroup, seed.GroupA.Id, futureAt, null)));
        Values(await Controller(db, seed.StudentAUser, clock, "Student").My()).Select(a => a.Title).Should().NotContain("Tomorrow");
        Values(await teacher.My()).Single(a => a.Id == future.Id).IsFuture.Should().BeTrue();
        clock.Set(new DateTimeOffset(2026, 7, 16, 9, 0, 0, TimeSpan.Zero));
        Values(await Controller(db, seed.StudentAUser, clock, "Student").My()).Select(a => a.Title).Should().Contain("Tomorrow");

        var expired = Value(await teacher.Create(new("Expired", "Old", AnnouncementAudienceType.SubjectGroup, seed.GroupA.Id, clock.GetUtcNow().UtcDateTime.AddMinutes(-2), clock.GetUtcNow().UtcDateTime.AddMinutes(-1))));
        Values(await Controller(db, seed.StudentAUser, clock, "Student").My()).Select(a => a.Title).Should().NotContain("Expired");
        Values(await teacher.My()).Single(a => a.Id == expired.Id).IsExpired.Should().BeTrue();

        var student = Controller(db, seed.StudentAUser, clock, "Student");
        Value(await student.My()).UnreadCount.Should().Be(2);
        (await student.Read(thai.Id)).Should().BeOfType<NoContentResult>();
        (await student.Read(thai.Id)).Should().BeOfType<NoContentResult>();
        Value(await student.My()).UnreadCount.Should().Be(1);
        (await db.AnnouncementReads.CountAsync(r => r.AnnouncementId == thai.Id && r.UserId == seed.StudentAUser.Id)).Should().Be(1);
    }

    [Fact]
    public async Task AttachmentAuthorization_FollowsAnnouncementAudience()
    {
        await using var db = Context(); var seed = await Seed(db); var clock = new TestTimeProvider(new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero)); var storage = new MemoryStorage();
        var teacher = Controller(db, seed.TeacherUser, clock, "Teacher");
        var announcement = Value(await teacher.Create(new("Files", "PDF", AnnouncementAudienceType.SubjectGroup, seed.GroupA.Id, null, null)));
        var files = Files(db, seed.TeacherUser, clock, storage, "Teacher");
        var uploaded = ((CreatedResult)(await files.UploadToAnnouncement(announcement.Id, FormFile("ประกาศ.pdf"), null, default)).Result!).Value.Should().BeOfType<FileResourceDto>().Subject;
        (await Files(db, seed.StudentAUser, clock, storage, "Student").Download(uploaded.Id, default)).Should().BeOfType<FileStreamResult>();
        (await Files(db, seed.StudentBUser, clock, storage, "Student").Download(uploaded.Id, default)).Should().BeOfType<ForbidResult>();
    }

    private static AnnouncementsController Controller(ApplicationDbContext db, User user, TimeProvider clock, params string[] roles)
    {
        var c = new AnnouncementsController(new AnnouncementRepository(db), new UserRepository(db), new MemoryStorage(), clock, NullLogger<AnnouncementsController>.Instance);
        SetUser(c, user, roles); return c;
    }

    private static FilesController Files(ApplicationDbContext db, User user, TimeProvider clock, IFileStorage storage, params string[] roles)
    {
        var c = new FilesController(new FileResourceRepository(db), new AnnouncementRepository(db), storage, new UserRepository(db), clock, NullLogger<FilesController>.Instance);
        SetUser(c, user, roles); return c;
    }

    private static void SetUser(ControllerBase c, User user, IEnumerable<string> roles) { var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id.ToString()) }; claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r))); c.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) } }; }
    private static T Value<T>(ActionResult<T> result) => result.Value ?? (T)((ObjectResult)result.Result!).Value!;
    private static IReadOnlyList<AnnouncementDto> Values(ActionResult<AnnouncementFeedDto> result) => Value(result).Announcements;

    private sealed record SeedData(User AdminUser, User TeacherUser, User OtherTeacherUser, User StudentAUser, User StudentBUser, User ParentUser, SchoolClass ClassA, SchoolClass ClassB, SubjectGroup GroupA, SubjectGroup GroupB);
    private static async Task<SeedData> Seed(ApplicationDbContext db)
    {
        var teacher = Teacher("t1"); var otherTeacher = Teacher("t2"); var studentA = Student("A"); var studentB = Student("B"); var guardian = new Guardian { Name = new PersonName { FirstName = "P", LastName = "One" } }; var year = new SchoolYear { Name = "Y", StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) };
        db.AddRange(teacher, otherTeacher, studentA, studentB, guardian, year); await db.SaveChangesAsync();
        var classA = new SchoolClass { Name = "ม.1/1", SchoolYearId = year.Id, HomeroomTeacherId = teacher.Id };
        var classB = new SchoolClass { Name = "ม.1/2", SchoolYearId = year.Id, HomeroomTeacherId = otherTeacher.Id };
        var term = new Term { Name = "T", SchoolYearId = year.Id, StartDate = year.StartDate, EndDate = year.EndDate };
        var subject = new Subject { Code = "M", NameEnglish = "Math", NameThai = "คณิตศาสตร์" };
        db.AddRange(classA, classB, term, subject); await db.SaveChangesAsync();
        var groupA = new SubjectGroup { Name = "กลุ่มคณิตศาสตร์", SubjectId = subject.Id, TermId = term.Id, TeacherId = teacher.Id };
        var groupB = new SubjectGroup { Name = "กลุ่มอื่น", SubjectId = subject.Id, TermId = term.Id, TeacherId = otherTeacher.Id };
        db.AddRange(groupA, groupB); await db.SaveChangesAsync();
        db.AddRange(new ClassMembership { StudentId = studentA.Id, ClassId = classA.Id, JoinedDate = DateTime.UtcNow }, new ClassMembership { StudentId = studentB.Id, ClassId = classB.Id, JoinedDate = DateTime.UtcNow }, new Enrollment { StudentId = studentA.Id, SubjectGroupId = groupA.Id, EnrolledDate = DateTime.UtcNow }, new Enrollment { StudentId = studentB.Id, SubjectGroupId = groupB.Id, EnrolledDate = DateTime.UtcNow }, new StudentGuardian { StudentId = studentA.Id, GuardianId = guardian.Id, Relationship = "parent" });
        var admin = User("admin"); var teacherUser = User("teacher", teacherId: teacher.Id); var otherTeacherUser = User("other", teacherId: otherTeacher.Id); var studentAUser = User("a", studentId: studentA.Id); var studentBUser = User("b", studentId: studentB.Id); var parentUser = User("parent", guardianId: guardian.Id);
        db.AddRange(admin, teacherUser, otherTeacherUser, studentAUser, studentBUser, parentUser); await db.SaveChangesAsync();
        return new(admin, teacherUser, otherTeacherUser, studentAUser, studentBUser, parentUser, classA, classB, groupA, groupB);
    }

    private static Teacher Teacher(string id) => new() { Name = new PersonName { FirstName = id, LastName = "Teacher" }, Email = $"{id}@test" };
    private static Student Student(string id) => new() { Name = new PersonName { FirstName = id, LastName = "Student" }, Email = $"{id}@test", StudentId = id, IdCardNumber = $"ID{id}", DateOfBirth = DateTime.Today, EnrollmentDate = DateTime.Today, PhoneNumber = "0", Address = "x" };
    private static User User(string id, int? teacherId = null, int? studentId = null, int? guardianId = null) => new() { Email = $"{id}@test", PasswordHash = "x", TeacherId = teacherId, StudentId = studentId, GuardianId = guardianId };
    private static FormFile FormFile(string name) { var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name) { Headers = new HeaderDictionary(), ContentType = "application/pdf" }; }

    private sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider { private DateTimeOffset value = now; public override DateTimeOffset GetUtcNow() => value; public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc; public void Set(DateTimeOffset next) => value = next; }
    private sealed class MemoryStorage : IFileStorage
    {
        private readonly HashSet<string> names = [];
        public Task<StoredFile> StoreAsync(Stream source, string originalFileName, long sizeBytes, CancellationToken cancellationToken = default) { var name = $"{Guid.NewGuid():N}.pdf"; names.Add(name); return Task.FromResult(new StoredFile("test", name, sizeBytes)); }
        public Task<Stream?> OpenReadAsync(string storedPath, string storedName, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(names.Contains(storedName) ? new MemoryStream([1]) : null);
        public Task DeleteAsync(string storedPath, string storedName, CancellationToken cancellationToken = default) { names.Remove(storedName); return Task.CompletedTask; }
    }
}
