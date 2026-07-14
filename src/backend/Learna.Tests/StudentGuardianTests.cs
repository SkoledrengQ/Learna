using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Learna.Core.Entities;
using Learna.Infrastructure.Data;
using Learna.Infrastructure.Repositories;

namespace Learna.Tests;

public class StudentGuardianTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Student MakeStudent(string studentId, string thaiFirstName = "สมชาย", string thaiLastName = "ศรีสุข") => new()
    {
        Name = new PersonName
        {
            Title = "เด็กชาย",
            FirstName = thaiFirstName,
            LastName = thaiLastName,
            FirstNameEnglish = "Somchai",
            LastNameEnglish = "Srisuk",
            Nickname = "ชัย"
        },
        Email = $"{studentId}@learna.test",
        StudentId = studentId,
        IdCardNumber = $"ID-{studentId}",
        DateOfBirth = new DateTime(2010, 1, 1),
        EnrollmentDate = DateTime.UtcNow,
        PhoneNumber = "081-000-0000",
        Address = "123 Sukhumvit Rd, Bangkok"
    };

    private static Guardian MakeGuardian(string firstName) => new()
    {
        Name = new PersonName { FirstName = firstName, LastName = "ศรีสุข" }
    };

    [Fact]
    public async Task Student_RoundTrips_ThaiName_ThroughRepository()
    {
        await using var context = CreateContext();
        var repository = new StudentRepository(context);

        var student = MakeStudent("2026-0001");
        var created = await repository.CreateAsync(student);

        var fetched = await repository.GetByIdAsync(created.Id);

        fetched.Should().NotBeNull();
        fetched!.Name.FirstName.Should().Be("สมชาย");
        fetched.Name.LastName.Should().Be("ศรีสุข");
        fetched.Name.Nickname.Should().Be("ชัย");
        fetched.Name.Title.Should().Be("เด็กชาย");
        fetched.Name.FirstNameEnglish.Should().Be("Somchai");
        fetched.CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task StudentIdExistsAsync_DetectsDuplicate()
    {
        await using var context = CreateContext();
        var repository = new StudentRepository(context);
        await repository.CreateAsync(MakeStudent("2026-0002"));

        (await repository.StudentIdExistsAsync("2026-0002")).Should().BeTrue();
        (await repository.StudentIdExistsAsync("2026-9999")).Should().BeFalse();
    }

    [Fact]
    public async Task Student_CanHaveTwoGuardians()
    {
        await using var context = CreateContext();
        var studentRepository = new StudentRepository(context);
        var guardianRepository = new GuardianRepository(context);

        var student = await studentRepository.CreateAsync(MakeStudent("2026-0003"));
        var mother = await guardianRepository.CreateAsync(MakeGuardian("Malee"));
        var father = await guardianRepository.CreateAsync(MakeGuardian("Somsak"));

        await guardianRepository.LinkAsync(new StudentGuardian
        {
            StudentId = student.Id,
            GuardianId = mother.Id,
            Relationship = "mother",
            IsPrimaryContact = true
        });
        await guardianRepository.LinkAsync(new StudentGuardian
        {
            StudentId = student.Id,
            GuardianId = father.Id,
            Relationship = "father",
            IsPrimaryContact = false
        });

        var withGuardians = await studentRepository.GetByIdWithGuardiansAsync(student.Id);

        withGuardians!.StudentGuardians.Should().HaveCount(2);
        withGuardians.StudentGuardians.Select(sg => sg.Guardian.Name.FirstName)
            .Should().BeEquivalentTo(new[] { "Malee", "Somsak" });
    }

    [Fact]
    public async Task Guardian_CanBeLinkedToTwoStudents()
    {
        await using var context = CreateContext();
        var studentRepository = new StudentRepository(context);
        var guardianRepository = new GuardianRepository(context);

        var studentA = await studentRepository.CreateAsync(MakeStudent("2026-0004"));
        var studentB = await studentRepository.CreateAsync(MakeStudent("2026-0005"));
        var sharedGuardian = await guardianRepository.CreateAsync(MakeGuardian("Malee"));

        await guardianRepository.LinkAsync(new StudentGuardian
        {
            StudentId = studentA.Id,
            GuardianId = sharedGuardian.Id,
            Relationship = "mother",
            IsPrimaryContact = true
        });
        await guardianRepository.LinkAsync(new StudentGuardian
        {
            StudentId = studentB.Id,
            GuardianId = sharedGuardian.Id,
            Relationship = "mother",
            IsPrimaryContact = true
        });

        var linkToA = await guardianRepository.GetLinkAsync(studentA.Id, sharedGuardian.Id);
        var linkToB = await guardianRepository.GetLinkAsync(studentB.Id, sharedGuardian.Id);

        linkToA.Should().NotBeNull();
        linkToB.Should().NotBeNull();
    }

    [Fact]
    public async Task UnlinkAsync_RemovesLink_ButKeepsGuardian()
    {
        await using var context = CreateContext();
        var studentRepository = new StudentRepository(context);
        var guardianRepository = new GuardianRepository(context);

        var student = await studentRepository.CreateAsync(MakeStudent("2026-0006"));
        var guardian = await guardianRepository.CreateAsync(MakeGuardian("Malee"));
        await guardianRepository.LinkAsync(new StudentGuardian
        {
            StudentId = student.Id,
            GuardianId = guardian.Id,
            Relationship = "mother",
            IsPrimaryContact = true
        });

        var unlinked = await guardianRepository.UnlinkAsync(student.Id, guardian.Id);

        unlinked.Should().BeTrue();
        (await guardianRepository.GetLinkAsync(student.Id, guardian.Id)).Should().BeNull();
        (await guardianRepository.GetByIdAsync(guardian.Id)).Should().NotBeNull();
    }
}
