using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Learna.Api.Controllers;
using Learna.Api.DTOs;
using Learna.Core.Entities;
using Learna.Infrastructure.Data;
using Learna.Infrastructure.Repositories;

namespace Learna.Tests;

public class RelationalCoreTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static T Created<T>(ActionResult<T> actionResult)
    {
        var created = actionResult.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        return (T)created.Value!;
    }

    private static T Ok<T>(ActionResult<T> actionResult)
    {
        var ok = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        return (T)ok.Value!;
    }

    private static async Task<Student> CreateStudentAsync(ApplicationDbContext context, string studentId, string firstName = "Somchai", string lastName = "Srisuk")
    {
        var student = new Student
        {
            Name = new PersonName { FirstName = firstName, LastName = lastName },
            Email = $"{studentId}@learna.test",
            StudentId = studentId,
            IdCardNumber = $"ID-{studentId}",
            DateOfBirth = new DateTime(2015, 1, 1),
            EnrollmentDate = new DateTime(2026, 5, 1),
            PhoneNumber = "0000000000",
            Address = "Bangkok"
        };
        context.Students.Add(student);
        await context.SaveChangesAsync();
        return student;
    }

    private static async Task<(SchoolYear year, Term term)> CreateYearAndTermAsync(ApplicationDbContext context)
    {
        var year = new SchoolYear
        {
            Name = "2026/2027",
            StartDate = new DateTime(2026, 5, 1),
            EndDate = new DateTime(2027, 3, 31)
        };
        context.SchoolYears.Add(year);
        await context.SaveChangesAsync();

        var term = new Term
        {
            SchoolYearId = year.Id,
            Name = "Semester 1",
            StartDate = new DateTime(2026, 5, 1),
            EndDate = new DateTime(2026, 10, 15)
        };
        context.Terms.Add(term);
        await context.SaveChangesAsync();

        return (year, term);
    }

    private static ClassesController CreateClassesController(ApplicationDbContext context) => new(
        new SchoolClassRepository(context),
        new ClassMembershipRepository(context),
        new StudentRepository(context),
        new SchoolYearRepository(context),
        new TeacherRepository(context));

    private static SubjectGroupsController CreateSubjectGroupsController(ApplicationDbContext context) => new(
        new SubjectGroupRepository(context),
        new EnrollmentRepository(context),
        new SchoolClassRepository(context),
        new ClassMembershipRepository(context),
        new StudentRepository(context),
        new SubjectRepository(context),
        new TermRepository(context),
        new TeacherRepository(context));

    [Fact]
    public async Task MoveStudent_ClosesOldMembership_OpensNewOne_PreservesHistory()
    {
        await using var context = CreateContext();
        var (year, _) = await CreateYearAndTermAsync(context);
        var student = await CreateStudentAsync(context, "S001");

        var classesController = CreateClassesController(context);

        var classA = Created(await classesController.Create(new CreateSchoolClassDto(year.Id, "1A", null, null)));
        var classB = Created(await classesController.Create(new CreateSchoolClassDto(year.Id, "1B", null, null)));

        await classesController.AddMember(classA.Id, new AddClassMemberDto(student.Id, new DateTime(2026, 5, 1)));

        var moved = Ok(await classesController.MoveMember(classA.Id, student.Id, new MoveClassMemberDto(classB.Id, new DateTime(2026, 8, 1))));
        moved.Should().NotBeNull();

        var oldClassMembers = Ok(await classesController.GetMembers(classA.Id, includeHistorical: true)).ToList();
        oldClassMembers.Should().ContainSingle();
        oldClassMembers[0].LeftDate.Should().Be(new DateTime(2026, 8, 1));

        var oldClassActiveMembers = Ok(await classesController.GetMembers(classA.Id, includeHistorical: false)).ToList();
        oldClassActiveMembers.Should().BeEmpty();

        var newClassMembers = Ok(await classesController.GetMembers(classB.Id, includeHistorical: false)).ToList();
        newClassMembers.Should().ContainSingle();
        newClassMembers[0].LeftDate.Should().BeNull();
        newClassMembers[0].JoinedDate.Should().Be(new DateTime(2026, 8, 1));
        newClassMembers[0].Student.Id.Should().Be(student.Id);
    }

    [Fact]
    public async Task AddMember_SecondActiveMembershipInSameSchoolYear_IsRejected()
    {
        await using var context = CreateContext();
        var (year, _) = await CreateYearAndTermAsync(context);
        var student = await CreateStudentAsync(context, "S002");

        var classesController = CreateClassesController(context);
        var classA = Created(await classesController.Create(new CreateSchoolClassDto(year.Id, "1A", null, null)));
        var classB = Created(await classesController.Create(new CreateSchoolClassDto(year.Id, "1B", null, null)));

        await classesController.AddMember(classA.Id, new AddClassMemberDto(student.Id, null));

        var secondAdd = await classesController.AddMember(classB.Id, new AddClassMemberDto(student.Id, null));

        secondAdd.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task AddEnrollment_DuplicateActiveEnrollment_IsRejected()
    {
        await using var context = CreateContext();
        var (_, term) = await CreateYearAndTermAsync(context);
        var student = await CreateStudentAsync(context, "S003");
        var subject = new Subject { Code = "math", NameEnglish = "Mathematics", NameThai = "คณิตศาสตร์" };
        context.Subjects.Add(subject);
        await context.SaveChangesAsync();

        var groupsController = CreateSubjectGroupsController(context);
        var group = Created(await groupsController.Create(new CreateSubjectGroupDto(subject.Id, term.Id, "Math Group", null)));

        var first = await groupsController.AddEnrollment(group.Id, new EnrollStudentDto(student.Id, null));
        first.Result.Should().BeOfType<CreatedAtActionResult>();

        var second = await groupsController.AddEnrollment(group.Id, new EnrollStudentDto(student.Id, null));
        second.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task EnrollClass_IsIdempotent_SkipsAlreadyEnrolledStudents()
    {
        await using var context = CreateContext();
        var (year, term) = await CreateYearAndTermAsync(context);
        var studentA = await CreateStudentAsync(context, "S004", "Wipa", "Jaidee");
        var studentB = await CreateStudentAsync(context, "S005", "Anon", "Suksan");
        var subject = new Subject { Code = "math2", NameEnglish = "Mathematics" };
        context.Subjects.Add(subject);
        await context.SaveChangesAsync();

        var classesController = CreateClassesController(context);
        var schoolClass = Created(await classesController.Create(new CreateSchoolClassDto(year.Id, "ม.1/1", null, null)));
        await classesController.AddMember(schoolClass.Id, new AddClassMemberDto(studentA.Id, null));
        await classesController.AddMember(schoolClass.Id, new AddClassMemberDto(studentB.Id, null));

        var groupsController = CreateSubjectGroupsController(context);
        var group = Created(await groupsController.Create(new CreateSubjectGroupDto(subject.Id, term.Id, "ม.1/1 - คณิตศาสตร์", null)));

        var firstRun = Ok(await groupsController.EnrollClass(group.Id, schoolClass.Id));
        firstRun.Enrolled.Should().Be(2);
        firstRun.Skipped.Should().Be(0);

        var secondRun = Ok(await groupsController.EnrollClass(group.Id, schoolClass.Id));
        secondRun.Enrolled.Should().Be(0);
        secondRun.Skipped.Should().Be(2);

        var enrollments = Ok(await groupsController.GetEnrollments(group.Id, includeHistorical: false)).ToList();
        enrollments.Should().HaveCount(2);
    }

    [Fact]
    public async Task SchoolClassAndSubjectGroup_RoundTrip_ThaiNames()
    {
        await using var context = CreateContext();
        var (year, term) = await CreateYearAndTermAsync(context);
        var subject = new Subject { Code = "thai", NameEnglish = "Thai Language", NameThai = "ภาษาไทย" };
        context.Subjects.Add(subject);
        await context.SaveChangesAsync();

        var classesController = CreateClassesController(context);
        var schoolClass = Created(await classesController.Create(new CreateSchoolClassDto(year.Id, "ม.1/1", "ห้องเรียนหลัก", null)));
        schoolClass.Name.Should().Be("ม.1/1");
        schoolClass.Description.Should().Be("ห้องเรียนหลัก");

        var fetchedClass = Ok(await classesController.GetById(schoolClass.Id));
        fetchedClass.Name.Should().Be("ม.1/1");

        var groupsController = CreateSubjectGroupsController(context);
        var group = Created(await groupsController.Create(new CreateSubjectGroupDto(subject.Id, term.Id, "Spanish Beginner Group", null)));
        var group2 = Created(await groupsController.Create(new CreateSubjectGroupDto(subject.Id, term.Id, "German Group", null)));

        var fetchedGroup = Ok(await groupsController.GetById(group.Id));
        fetchedGroup.Name.Should().Be("Spanish Beginner Group");
        group2.Name.Should().Be("German Group");
    }

    [Fact]
    public async Task TwoStudentsInSameClass_CanBeEnrolledInDifferentSubjectGroups()
    {
        await using var context = CreateContext();
        var (year, term) = await CreateYearAndTermAsync(context);
        var studentA = await CreateStudentAsync(context, "S006", "Somsri", "Deejai");
        var studentB = await CreateStudentAsync(context, "S007", "Kittipong", "Meesuk");
        var mathSubject = new Subject { Code = "math3", NameEnglish = "Mathematics" };
        var spanishSubject = new Subject { Code = "spanish", NameEnglish = "Spanish" };
        var germanSubject = new Subject { Code = "german", NameEnglish = "German" };
        context.Subjects.AddRange(mathSubject, spanishSubject, germanSubject);
        await context.SaveChangesAsync();

        var classesController = CreateClassesController(context);
        var schoolClass = Created(await classesController.Create(new CreateSchoolClassDto(year.Id, "ม.1/1", null, null)));
        await classesController.AddMember(schoolClass.Id, new AddClassMemberDto(studentA.Id, null));
        await classesController.AddMember(schoolClass.Id, new AddClassMemberDto(studentB.Id, null));

        var groupsController = CreateSubjectGroupsController(context);
        var mathGroup = Created(await groupsController.Create(new CreateSubjectGroupDto(mathSubject.Id, term.Id, "ม.1/1 - คณิตศาสตร์", null)));
        var spanishGroup = Created(await groupsController.Create(new CreateSubjectGroupDto(spanishSubject.Id, term.Id, "Spanish Beginner Group", null)));
        var germanGroup = Created(await groupsController.Create(new CreateSubjectGroupDto(germanSubject.Id, term.Id, "German Group", null)));

        await groupsController.EnrollClass(mathGroup.Id, schoolClass.Id);
        await groupsController.AddEnrollment(spanishGroup.Id, new EnrollStudentDto(studentA.Id, null));
        await groupsController.AddEnrollment(germanGroup.Id, new EnrollStudentDto(studentB.Id, null));

        var mathRoster = Ok(await groupsController.GetEnrollments(mathGroup.Id, false)).Select(e => e.Student.Id).ToList();
        mathRoster.Should().BeEquivalentTo(new[] { studentA.Id, studentB.Id });

        var spanishRoster = Ok(await groupsController.GetEnrollments(spanishGroup.Id, false)).Select(e => e.Student.Id).ToList();
        spanishRoster.Should().BeEquivalentTo(new[] { studentA.Id });

        var germanRoster = Ok(await groupsController.GetEnrollments(germanGroup.Id, false)).Select(e => e.Student.Id).ToList();
        germanRoster.Should().BeEquivalentTo(new[] { studentB.Id });
    }
}
