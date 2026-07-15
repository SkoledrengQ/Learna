using System.Security.Claims;
using FluentAssertions;
using Learna.Api.Controllers;
using Learna.Api.DTOs;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;
using Learna.Infrastructure.Repositories;
using Learna.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Learna.Tests;

public class FileResourceTests
{
    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public void Model_HasExactlyOneTargetConstraint()
    {
        using var db = Context();
        db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(FileResource))!.GetCheckConstraints().Select(c => c.Name).Should().Contain("CK_FileResource_ExactlyOneTarget");
    }

    [Fact]
    public async Task Storage_RejectsSizeAndExtension_AndNeutralizesTraversal()
    {
        var root = Path.Combine(Path.GetTempPath(), $"learna-files-{Guid.NewGuid():N}");
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:RootPath"] = root, ["Storage:MaxFileSizeMb"] = "1", ["Storage:AllowedExtensions:0"] = ".pdf" }).Build();
        var storage = new LocalFileStorage(config);
        await FluentActions.Invoking(() => storage.StoreAsync(new MemoryStream(new byte[2]), "bad.exe", 2)).Should().ThrowAsync<FileStorageValidationException>().Where(e => e.ErrorCode == "FILE_TYPE_NOT_ALLOWED");
        await FluentActions.Invoking(() => storage.StoreAsync(new MemoryStream(new byte[2]), "ok.pdf", 1_048_577)).Should().ThrowAsync<FileStorageValidationException>().Where(e => e.ErrorCode == "FILE_TOO_LARGE");
        var stored = await storage.StoreAsync(new MemoryStream(new byte[] { 1, 2 }), "../../outside.pdf", 2);
        stored.StoredName.Should().MatchRegex("^[a-f0-9]{32}\\.pdf$");
        stored.StoredName.Should().NotContain("outside");
        await using var opened = await storage.OpenReadAsync(stored.StoredPath, stored.StoredName);
        opened.Should().NotBeNull();
        await opened!.DisposeAsync();
        Directory.Delete(root, true);
    }

    [Fact]
    public async Task PermissionMatrix_UnenrollmentAndThaiRoundTrip_AreEnforced()
    {
        await using var db = Context();
        var s = await SeedAsync(db); var storage = new MemoryStorage(); var repo = new FileResourceRepository(db);
        var upload = FormFile("แบบฝึกหัดคณิตศาสตร์.pdf");
        var teacherController = Controller(db, repo, storage, s.TeacherUser, "Teacher");
        var created = await teacherController.UploadToSubjectGroup(s.Group.Id, upload, "คำอธิบายภาษาไทย", default);
        created.Result.Should().BeOfType<CreatedResult>();
        var dto = ((CreatedResult)created.Result!).Value.Should().BeOfType<FileResourceDto>().Subject;
        dto.OriginalFileName.Should().Be("แบบฝึกหัดคณิตศาสตร์.pdf");
        (await db.FileResources.SingleAsync()).Description.Should().Be("คำอธิบายภาษาไทย");

        (await Controller(db, repo, storage, s.OtherTeacherUser, "Teacher").UploadToSubjectGroup(s.Group.Id, FormFile("x.pdf"), null, default)).Result.Should().BeOfType<ForbidResult>();
        (await Controller(db, repo, storage, s.StudentUser, "Student").Download(dto.Id, default)).Should().BeOfType<FileStreamResult>();
        (await Controller(db, repo, storage, s.UnenrolledUser, "Student").Download(dto.Id, default)).Should().BeOfType<ForbidResult>();
        (await Controller(db, repo, storage, s.ParentUser, "Parent").Download(dto.Id, default)).Should().BeOfType<FileStreamResult>();
        (await Controller(db, repo, storage, s.OtherParentUser, "Parent").Download(dto.Id, default)).Should().BeOfType<ForbidResult>();

        (await db.Enrollments.SingleAsync(e => e.StudentId == s.Student.Id)).UnenrolledDate = DateTime.UtcNow;
        await db.SaveChangesAsync();
        (await Controller(db, repo, storage, s.StudentUser, "Student").ListSubjectGroup(s.Group.Id)).Result.Should().BeOfType<ForbidResult>();
        (await Controller(db, repo, storage, s.StudentUser, "Student").Download(dto.Id, default)).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Delete_RemovesDatabaseAndDisk_AndDownloadReturnsNotFound()
    {
        await using var db = Context(); var s = await SeedAsync(db); var storage = new MemoryStorage(); var repo = new FileResourceRepository(db);
        var controller = Controller(db, repo, storage, s.TeacherUser, "Teacher");
        var created = await controller.UploadToSubjectGroup(s.Group.Id, FormFile("notes.pdf"), null, default);
        var id = ((FileResourceDto)((CreatedResult)created.Result!).Value!).Id;
        (await controller.Delete(id, default)).Should().BeOfType<NoContentResult>();
        storage.Exists.Should().BeFalse();
        (await controller.Download(id, default)).Should().BeOfType<NotFoundResult>();
    }

    private static FilesController Controller(ApplicationDbContext db, IFileResourceRepository repo, IFileStorage storage, User user, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id.ToString()) }; claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        var controller = new FilesController(repo, storage, new UserRepository(db), NullLogger<FilesController>.Instance);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) } };
        return controller;
    }

    private static FormFile FormFile(string name) { var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name) { Headers = new HeaderDictionary(), ContentType = "application/pdf" }; }

    private sealed record SeedData(User TeacherUser, User OtherTeacherUser, User StudentUser, User UnenrolledUser, User ParentUser, User OtherParentUser, Student Student, SubjectGroup Group);

    private static async Task<SeedData> SeedAsync(ApplicationDbContext db)
    {
        var teacher = new Teacher { Name = new PersonName { FirstName = "T", LastName = "One" }, Email = "t1@test" }; var otherTeacher = new Teacher { Name = new PersonName { FirstName = "T", LastName = "Two" }, Email = "t2@test" };
        var student = Student("S1"); var unenrolled = Student("S2"); var guardian = new Guardian { Name = new PersonName { FirstName = "Parent", LastName = "One" } }; var otherGuardian = new Guardian { Name = new PersonName { FirstName = "Parent", LastName = "Two" } }; var year = new SchoolYear { Name = "Y", StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) };
        db.AddRange(teacher, otherTeacher, student, unenrolled, guardian, otherGuardian, year); await db.SaveChangesAsync();
        var term = new Term { Name = "T", SchoolYearId = year.Id, StartDate = year.StartDate, EndDate = year.EndDate }; var subject = new Subject { Code = "M", NameEnglish = "Math" }; db.AddRange(term, subject); await db.SaveChangesAsync();
        var group = new SubjectGroup { Name = "Math group", SubjectId = subject.Id, TermId = term.Id, TeacherId = teacher.Id }; db.Add(group); await db.SaveChangesAsync();
        db.Enrollments.Add(new Enrollment { StudentId = student.Id, SubjectGroupId = group.Id, EnrolledDate = DateTime.UtcNow }); db.StudentGuardians.Add(new StudentGuardian { StudentId = student.Id, GuardianId = guardian.Id, Relationship = "parent" });
        var teacherUser = new User { Email = "tu@test", PasswordHash = "x", TeacherId = teacher.Id }; var otherTeacherUser = new User { Email = "ot@test", PasswordHash = "x", TeacherId = otherTeacher.Id }; var studentUser = new User { Email = "su@test", PasswordHash = "x", StudentId = student.Id }; var unenrolledUser = new User { Email = "uu@test", PasswordHash = "x", StudentId = unenrolled.Id }; var parentUser = new User { Email = "pu@test", PasswordHash = "x", GuardianId = guardian.Id }; var otherParentUser = new User { Email = "op@test", PasswordHash = "x", GuardianId = otherGuardian.Id }; db.AddRange(teacherUser, otherTeacherUser, studentUser, unenrolledUser, parentUser, otherParentUser); await db.SaveChangesAsync();
        return new SeedData(teacherUser, otherTeacherUser, studentUser, unenrolledUser, parentUser, otherParentUser, student, group);
    }
    private static Student Student(string id) => new() { Name = new PersonName { FirstName = id, LastName = "Student" }, Email = $"{id}@test", StudentId = id, IdCardNumber = $"ID{id}", DateOfBirth = DateTime.Today, EnrollmentDate = DateTime.Today, PhoneNumber = "0", Address = "x" };

    private sealed class MemoryStorage : IFileStorage
    {
        public bool Exists { get; private set; }
        public Task<StoredFile> StoreAsync(Stream source, string originalFileName, long sizeBytes, CancellationToken cancellationToken = default) { Exists = true; return Task.FromResult(new StoredFile("test", "generated.pdf", sizeBytes)); }
        public Task<Stream?> OpenReadAsync(string storedPath, string storedName, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(Exists ? new MemoryStream(new byte[] { 1 }) : null);
        public Task DeleteAsync(string storedPath, string storedName, CancellationToken cancellationToken = default) { Exists = false; return Task.CompletedTask; }
    }
}
