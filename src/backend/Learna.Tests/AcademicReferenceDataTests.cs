using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Learna.Api.Controllers;
using Learna.Api.DTOs;
using Learna.Core.Entities;
using Learna.Infrastructure.Data;
using Learna.Infrastructure.Repositories;

namespace Learna.Tests;

public class AcademicReferenceDataTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Teacher_RoundTrips_ThaiName_ThroughRepository()
    {
        await using var context = CreateContext();
        var repository = new TeacherRepository(context);

        var teacher = new Teacher
        {
            Name = new PersonName
            {
                Title = "ครู",
                FirstName = "วิภา",
                LastName = "ใจดี",
                FirstNameEnglish = "Wipa",
                LastNameEnglish = "Jaidee",
                Nickname = "ครูวิ"
            },
            Email = "wipa@learna.test"
        };

        var created = await repository.CreateAsync(teacher);
        var fetched = await repository.GetByIdAsync(created.Id);

        fetched.Should().NotBeNull();
        fetched!.Name.FirstName.Should().Be("วิภา");
        fetched.Name.LastName.Should().Be("ใจดี");
        fetched.Name.Title.Should().Be("ครู");
        fetched.CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task Subject_RoundTrips_ThaiName_ThroughRepository()
    {
        await using var context = CreateContext();
        var repository = new SubjectRepository(context);

        var subject = new Subject
        {
            Code = "math",
            NameEnglish = "Mathematics",
            NameThai = "คณิตศาสตร์"
        };

        var created = await repository.CreateAsync(subject);
        var fetched = await repository.GetByIdAsync(created.Id);

        fetched.Should().NotBeNull();
        fetched!.NameThai.Should().Be("คณิตศาสตร์");
        fetched.NameEnglish.Should().Be("Mathematics");
    }

    [Fact]
    public async Task SubjectCodeExistsAsync_DetectsDuplicate()
    {
        await using var context = CreateContext();
        var repository = new SubjectRepository(context);
        await repository.CreateAsync(new Subject { Code = "math", NameEnglish = "Mathematics" });

        (await repository.CodeExistsAsync("math")).Should().BeTrue();
        (await repository.CodeExistsAsync("science")).Should().BeFalse();
    }

    [Fact]
    public async Task AddTerm_WithinSchoolYearDates_Succeeds()
    {
        await using var context = CreateContext();
        var schoolYearRepository = new SchoolYearRepository(context);
        var termRepository = new TermRepository(context);
        var controller = new SchoolYearsController(schoolYearRepository, termRepository);

        var schoolYear = await schoolYearRepository.CreateAsync(new SchoolYear
        {
            Name = "2026/2027",
            StartDate = new DateTime(2026, 5, 1),
            EndDate = new DateTime(2027, 3, 31)
        });

        var result = await controller.AddTerm(schoolYear.Id, new CreateTermDto(
            "Semester 1",
            new DateTime(2026, 5, 1),
            new DateTime(2026, 10, 15)
        ));

        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task AddTerm_OutsideSchoolYearDates_IsRejected()
    {
        await using var context = CreateContext();
        var schoolYearRepository = new SchoolYearRepository(context);
        var termRepository = new TermRepository(context);
        var controller = new SchoolYearsController(schoolYearRepository, termRepository);

        var schoolYear = await schoolYearRepository.CreateAsync(new SchoolYear
        {
            Name = "2026/2027",
            StartDate = new DateTime(2026, 5, 1),
            EndDate = new DateTime(2027, 3, 31)
        });

        var result = await controller.AddTerm(schoolYear.Id, new CreateTermDto(
            "Invalid Term",
            new DateTime(2026, 1, 1),  // before school year starts
            new DateTime(2026, 6, 1)
        ));

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
