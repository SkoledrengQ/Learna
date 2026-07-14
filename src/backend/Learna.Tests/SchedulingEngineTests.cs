using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Learna.Api.Controllers;
using Learna.Api.DTOs;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;
using Learna.Infrastructure.Repositories;
using Learna.Infrastructure.Services;

namespace Learna.Tests;

public class SchedulingEngineTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ILessonSchedulingService CreateSchedulingService(ApplicationDbContext context) => new LessonSchedulingService(
        context,
        new SubjectGroupRepository(context),
        new TermRepository(context),
        new RoomRepository(context),
        new TeacherRepository(context),
        new LessonRuleRepository(context),
        new LessonRepository(context),
        new EnrollmentRepository(context));

    private static LessonRulesController CreateRulesController(ApplicationDbContext context, ILessonSchedulingService service) => new(
        new LessonRuleRepository(context),
        new SubjectGroupRepository(context),
        service);

    private static LessonsController CreateLessonsController(ApplicationDbContext context, ILessonSchedulingService service) => new(
        new LessonRepository(context),
        service);

    private static ScheduleController CreateScheduleController(ApplicationDbContext context) => new(
        new LessonRepository(context),
        new EnrollmentRepository(context),
        new StudentRepository(context),
        new TeacherRepository(context),
        new RoomRepository(context),
        new UserRepository(context));

    private static RoomsController CreateRoomsController(ApplicationDbContext context) => new(new RoomRepository(context));

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

    private static async Task<Student> CreateStudentAsync(ApplicationDbContext context, string studentId)
    {
        var student = new Student
        {
            Name = new PersonName { FirstName = "Somchai", LastName = studentId },
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

    private static async Task<Teacher> CreateTeacherAsync(ApplicationDbContext context, string email)
    {
        var teacher = new Teacher
        {
            Name = new PersonName { FirstName = "Wipa", LastName = "Jaidee" },
            Email = email
        };
        context.Teachers.Add(teacher);
        await context.SaveChangesAsync();
        return teacher;
    }

    private static async Task<(SchoolYear year, Term term)> CreateYearAndTermAsync(ApplicationDbContext context, DateOnly start, DateOnly end)
    {
        var year = new SchoolYear
        {
            Name = "2026/2027",
            StartDate = start.ToDateTime(TimeOnly.MinValue),
            EndDate = end.ToDateTime(TimeOnly.MinValue)
        };
        context.SchoolYears.Add(year);
        await context.SaveChangesAsync();

        var term = new Term
        {
            SchoolYearId = year.Id,
            Name = "Semester 1",
            StartDate = start.ToDateTime(TimeOnly.MinValue),
            EndDate = end.ToDateTime(TimeOnly.MinValue)
        };
        context.Terms.Add(term);
        await context.SaveChangesAsync();

        return (year, term);
    }

    private static async Task<SubjectGroup> CreateSubjectGroupAsync(ApplicationDbContext context, int termId, string name, int? teacherId = null)
    {
        var subject = new Subject { Code = Guid.NewGuid().ToString("N")[..8], NameEnglish = "Mathematics" };
        context.Subjects.Add(subject);
        await context.SaveChangesAsync();

        var group = new SubjectGroup { SubjectId = subject.Id, TermId = termId, Name = name, TeacherId = teacherId };
        context.SubjectGroups.Add(group);
        await context.SaveChangesAsync();
        return group;
    }

    private static async Task EnrollAsync(ApplicationDbContext context, int studentId, int subjectGroupId, DateTime enrolledDate)
    {
        context.Enrollments.Add(new Enrollment { StudentId = studentId, SubjectGroupId = subjectGroupId, EnrolledDate = enrolledDate });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateRule_GeneratesCorrectOccurrenceCountAndDates_AcrossTerm()
    {
        await using var context = CreateContext();
        var termStart = new DateOnly(2030, 1, 1);
        var termEnd = new DateOnly(2030, 3, 31);
        var (_, term) = await CreateYearAndTermAsync(context, termStart, termEnd);
        var group = await CreateSubjectGroupAsync(context, term.Id, "Math Group");

        var service = CreateSchedulingService(context);
        var result = await service.CreateRuleAsync(group.Id, DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(9, 30), null, null, null, false);

        result.Rule.Should().NotBeNull();

        var expectedDates = new List<DateOnly>();
        for (var d = termStart; d <= termEnd; d = d.AddDays(1))
        {
            if (d.DayOfWeek == DayOfWeek.Monday) expectedDates.Add(d);
        }

        var lessons = await context.Lessons.Where(l => l.SourceRuleId == result.Rule!.Id).ToListAsync();
        lessons.Should().HaveCount(expectedDates.Count);
        lessons.Select(l => l.Date).Should().BeEquivalentTo(expectedDates);
        lessons.Should().OnlyContain(l => l.DayOfWeekMatches(DayOfWeek.Monday));
    }

    [Fact]
    public async Task UpdateRule_PreservesPastAndModifiedLessons_RegeneratesRestOnly()
    {
        await using var context = CreateContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var termStart = today.AddDays(-21);
        var termEnd = today.AddDays(42);
        var (_, term) = await CreateYearAndTermAsync(context, termStart, termEnd);
        var group = await CreateSubjectGroupAsync(context, term.Id, "Math Group");

        var service = CreateSchedulingService(context);
        var createResult = await service.CreateRuleAsync(group.Id, DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(9, 0), null, null, null, false);
        var ruleId = createResult.Rule!.Id;

        var allLessons = await context.Lessons.Where(l => l.SourceRuleId == ruleId).ToListAsync();
        var pastLessons = allLessons.Where(l => l.Date < today).ToList();
        var futureLessons = allLessons.Where(l => l.Date >= today).OrderBy(l => l.Date).ToList();
        pastLessons.Should().NotBeEmpty();
        futureLessons.Should().HaveCountGreaterThan(1);

        var exceptionLesson = futureLessons[0];
        var lessonsController = CreateLessonsController(context, service);
        var updateExceptionResult = await lessonsController.Update(exceptionLesson.Id, new UpdateLessonDto(
            exceptionLesson.Date, exceptionLesson.StartTime, exceptionLesson.EndTime, null, null, "room change exception", LessonStatus.Scheduled, false));
        Ok(updateExceptionResult);

        var updateRuleResult = await service.UpdateRuleAsync(ruleId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(10, 0), null, termStart, termEnd, false);
        updateRuleResult.Rule.Should().NotBeNull();

        // Past lessons untouched.
        foreach (var past in pastLessons)
        {
            var reloaded = await context.Lessons.FindAsync(past.Id);
            reloaded.Should().NotBeNull();
            reloaded!.StartTime.Should().Be(new TimeOnly(8, 0));
            reloaded.SourceRuleId.Should().Be(ruleId);
        }

        // The individually-modified future lesson kept its own exception, untouched by the rule edit.
        var reloadedException = await context.Lessons.FindAsync(exceptionLesson.Id);
        reloadedException.Should().NotBeNull();
        reloadedException!.StartTime.Should().Be(new TimeOnly(8, 0));
        reloadedException.IsModified.Should().BeTrue();
        reloadedException.Note.Should().Be("room change exception");

        // Sibling future (unmodified) lessons moved to the new rule time.
        var otherFutureLessons = await context.Lessons
            .Where(l => l.SourceRuleId == ruleId && l.Date >= today && l.Id != exceptionLesson.Id)
            .ToListAsync();
        otherFutureLessons.Should().NotBeEmpty();
        otherFutureLessons.Should().OnlyContain(l => l.StartTime == new TimeOnly(9, 0) && !l.IsModified);
    }

    [Fact]
    public async Task DeleteRule_DeletesFutureUnmodifiedLessons_NullsSourceRuleIdOnSurvivors()
    {
        await using var context = CreateContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var termStart = today.AddDays(-21);
        var termEnd = today.AddDays(42);
        var (_, term) = await CreateYearAndTermAsync(context, termStart, termEnd);
        var group = await CreateSubjectGroupAsync(context, term.Id, "Math Group");

        var service = CreateSchedulingService(context);
        var createResult = await service.CreateRuleAsync(group.Id, DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(9, 0), null, null, null, false);
        var ruleId = createResult.Rule!.Id;

        var allLessons = await context.Lessons.Where(l => l.SourceRuleId == ruleId).ToListAsync();
        var pastLessonIds = allLessons.Where(l => l.Date < today).Select(l => l.Id).ToList();
        var futureLessons = allLessons.Where(l => l.Date >= today).OrderBy(l => l.Date).ToList();

        var lessonsController = CreateLessonsController(context, service);
        var modifiedFuture = futureLessons[0];
        await lessonsController.Update(modifiedFuture.Id, new UpdateLessonDto(
            modifiedFuture.Date, modifiedFuture.StartTime, modifiedFuture.EndTime, null, null, "kept", LessonStatus.Scheduled, false));
        var unmodifiedFutureIds = futureLessons.Skip(1).Select(l => l.Id).ToList();

        var deleted = await service.DeleteRuleAsync(ruleId);
        deleted.Should().BeTrue();

        foreach (var id in pastLessonIds)
        {
            var reloaded = await context.Lessons.FindAsync(id);
            reloaded.Should().NotBeNull();
            reloaded!.SourceRuleId.Should().BeNull();
        }

        var reloadedModified = await context.Lessons.FindAsync(modifiedFuture.Id);
        reloadedModified.Should().NotBeNull();
        reloadedModified!.SourceRuleId.Should().BeNull();
        reloadedModified.Note.Should().Be("kept");

        foreach (var id in unmodifiedFutureIds)
        {
            var reloaded = await context.Lessons.FindAsync(id);
            reloaded.Should().BeNull();
        }
    }

    [Fact]
    public async Task CreateOneOffLesson_TeacherConflict_IsDetected_AndForceOverrides()
    {
        await using var context = CreateContext();
        var (_, term) = await CreateYearAndTermAsync(context, new DateOnly(2030, 1, 1), new DateOnly(2030, 3, 31));
        var teacher = await CreateTeacherAsync(context, "teacher1@learna.test");
        var group1 = await CreateSubjectGroupAsync(context, term.Id, "Group 1", teacher.Id);
        var group2 = await CreateSubjectGroupAsync(context, term.Id, "Group 2", teacher.Id);

        var service = CreateSchedulingService(context);
        var lessonsController = CreateLessonsController(context, service);

        var date = new DateOnly(2030, 1, 7);
        var first = await lessonsController.Create(new CreateLessonDto(group1.Id, date, new TimeOnly(8, 0), new TimeOnly(9, 0), null, null, null, false));
        Created(first);

        var second = await lessonsController.Create(new CreateLessonDto(group2.Id, date, new TimeOnly(8, 30), new TimeOnly(9, 30), null, null, null, false));
        var conflictResponse = second.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        var conflicts = ((ConflictResponseDto)conflictResponse.Value!).Conflicts.ToList();
        conflicts.Should().Contain(c => c.Type == ConflictType.Teacher);

        var forced = await lessonsController.Create(new CreateLessonDto(group2.Id, date, new TimeOnly(8, 30), new TimeOnly(9, 30), null, null, null, true));
        Created(forced);
    }

    [Fact]
    public async Task CreateOneOffLesson_RoomConflict_IsDetected()
    {
        await using var context = CreateContext();
        var (_, term) = await CreateYearAndTermAsync(context, new DateOnly(2030, 1, 1), new DateOnly(2030, 3, 31));
        var group1 = await CreateSubjectGroupAsync(context, term.Id, "Group 1");
        var group2 = await CreateSubjectGroupAsync(context, term.Id, "Group 2");
        var roomsController = CreateRoomsController(context);
        var room = Created(await roomsController.Create(new CreateRoomDto("ห้อง 204", null, null, null)));

        var service = CreateSchedulingService(context);
        var lessonsController = CreateLessonsController(context, service);

        var date = new DateOnly(2030, 1, 7);
        Created(await lessonsController.Create(new CreateLessonDto(group1.Id, date, new TimeOnly(8, 0), new TimeOnly(9, 0), room.Id, null, null, false)));

        var second = await lessonsController.Create(new CreateLessonDto(group2.Id, date, new TimeOnly(8, 30), new TimeOnly(9, 30), room.Id, null, null, false));
        var conflictResponse = second.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        var conflicts = ((ConflictResponseDto)conflictResponse.Value!).Conflicts.ToList();
        conflicts.Should().Contain(c => c.Type == ConflictType.Room);
    }

    [Fact]
    public async Task CreateOneOffLesson_StudentConflict_IsDetected()
    {
        await using var context = CreateContext();
        var (_, term) = await CreateYearAndTermAsync(context, new DateOnly(2030, 1, 1), new DateOnly(2030, 3, 31));
        var group1 = await CreateSubjectGroupAsync(context, term.Id, "Group 1");
        var group2 = await CreateSubjectGroupAsync(context, term.Id, "Group 2");
        var student = await CreateStudentAsync(context, "S100");
        await EnrollAsync(context, student.Id, group1.Id, new DateTime(2026, 1, 1));
        await EnrollAsync(context, student.Id, group2.Id, new DateTime(2026, 1, 1));

        var service = CreateSchedulingService(context);
        var lessonsController = CreateLessonsController(context, service);

        var date = new DateOnly(2030, 1, 7);
        Created(await lessonsController.Create(new CreateLessonDto(group1.Id, date, new TimeOnly(8, 0), new TimeOnly(9, 0), null, null, null, false)));

        var second = await lessonsController.Create(new CreateLessonDto(group2.Id, date, new TimeOnly(8, 30), new TimeOnly(9, 30), null, null, null, false));
        var conflictResponse = second.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        var conflicts = ((ConflictResponseDto)conflictResponse.Value!).Conflicts.ToList();
        conflicts.Should().Contain(c => c.Type == ConflictType.Student);
    }

    [Fact]
    public async Task CancelledLesson_IsExcludedFromConflicts()
    {
        await using var context = CreateContext();
        var (_, term) = await CreateYearAndTermAsync(context, new DateOnly(2030, 1, 1), new DateOnly(2030, 3, 31));
        var teacher = await CreateTeacherAsync(context, "teacher2@learna.test");
        var group1 = await CreateSubjectGroupAsync(context, term.Id, "Group 1", teacher.Id);
        var group2 = await CreateSubjectGroupAsync(context, term.Id, "Group 2", teacher.Id);

        var service = CreateSchedulingService(context);
        var lessonsController = CreateLessonsController(context, service);

        var date = new DateOnly(2030, 1, 7);
        var first = Created(await lessonsController.Create(new CreateLessonDto(group1.Id, date, new TimeOnly(8, 0), new TimeOnly(9, 0), null, null, null, false)));

        var cancelResult = await lessonsController.Update(first.Id, new UpdateLessonDto(
            date, new TimeOnly(8, 0), new TimeOnly(9, 0), null, null, null, LessonStatus.Cancelled, false));
        Ok(cancelResult);

        var second = await lessonsController.Create(new CreateLessonDto(group2.Id, date, new TimeOnly(8, 30), new TimeOnly(9, 30), null, null, null, false));
        Created(second);
    }

    [Fact]
    public async Task StudentSchedule_ReflectsActiveEnrollment_FutureLessonsDisappearOnUnenroll()
    {
        await using var context = CreateContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var termStart = today.AddDays(-7);
        var termEnd = today.AddDays(21);
        var (_, term) = await CreateYearAndTermAsync(context, termStart, termEnd);
        var group = await CreateSubjectGroupAsync(context, term.Id, "Math Group");
        var student = await CreateStudentAsync(context, "S200");

        var service = CreateSchedulingService(context);
        await service.CreateRuleAsync(group.Id, DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(9, 0), null, null, null, false);

        var enrollment = new Enrollment { StudentId = student.Id, SubjectGroupId = group.Id, EnrolledDate = termStart.ToDateTime(TimeOnly.MinValue) };
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync();

        var scheduleController = CreateScheduleController(context);
        var before = Ok(await scheduleController.GetStudentSchedule(student.Id, null, null)).ToList();
        before.Should().NotBeEmpty();

        var enrollmentRepository = new EnrollmentRepository(context);
        var active = await enrollmentRepository.GetActiveBySubjectGroupAndStudentAsync(group.Id, student.Id);
        await enrollmentRepository.CloseAsync(active!, DateTime.UtcNow);

        var after = Ok(await scheduleController.GetStudentSchedule(student.Id, null, null)).ToList();
        after.Should().BeEmpty();
    }

    [Fact]
    public async Task Room_And_LessonNote_RoundTrip_ThaiText()
    {
        await using var context = CreateContext();
        var (_, term) = await CreateYearAndTermAsync(context, new DateOnly(2030, 1, 1), new DateOnly(2030, 3, 31));
        var group = await CreateSubjectGroupAsync(context, term.Id, "Group 1");

        var roomsController = CreateRoomsController(context);
        var createdRoom = Created(await roomsController.Create(new CreateRoomDto("ห้อง 204", "อาคาร 2", "ห้องเรียนวิทยาศาสตร์", 30)));
        var fetchedRoom = Ok(await roomsController.GetById(createdRoom.Id));
        fetchedRoom.Name.Should().Be("ห้อง 204");
        fetchedRoom.Building.Should().Be("อาคาร 2");

        var service = CreateSchedulingService(context);
        var lessonsController = CreateLessonsController(context, service);
        var createdLesson = Created(await lessonsController.Create(new CreateLessonDto(
            group.Id, new DateOnly(2030, 1, 7), new TimeOnly(8, 0), new TimeOnly(9, 0), createdRoom.Id, null, "ครูลาป่วย มีครูสอนแทน", false)));

        var reloaded = await new LessonRepository(context).GetByIdAsync(createdLesson.Id);
        reloaded.Should().NotBeNull();
        reloaded!.Note.Should().Be("ครูลาป่วย มีครูสอนแทน");
        reloaded.Room!.Name.Should().Be("ห้อง 204");
    }
}

internal static class LessonTestExtensions
{
    public static bool DayOfWeekMatches(this Lesson lesson, DayOfWeek dayOfWeek) => lesson.Date.DayOfWeek == dayOfWeek;
}
