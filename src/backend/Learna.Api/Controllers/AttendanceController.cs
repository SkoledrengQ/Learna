using System.Security.Claims;
using Learna.Api.DTOs;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learna.Api.Controllers;

[Authorize]
[ApiController]
public class AttendanceController : ControllerBase
{
    private readonly ILessonRepository _lessons;
    private readonly IAttendanceRepository _attendance;
    private readonly IUserRepository _users;
    private readonly IStudentRepository _students;
    private readonly int _editWindowDays;

    public AttendanceController(ILessonRepository lessons, IAttendanceRepository attendance, IUserRepository users, IStudentRepository students, IConfiguration configuration)
    {
        _lessons = lessons; _attendance = attendance; _users = users; _students = students;
        // TODO: make this rule school-configurable when school settings are introduced.
        _editWindowDays = configuration.GetValue("Attendance:EditWindowDays", 7);
    }

    [HttpGet("api/lessons/{lessonId:int}/attendance")]
    public async Task<ActionResult<LessonAttendanceDto>> GetLesson(int lessonId)
    {
        var lesson = await _lessons.GetByIdAsync(lessonId);
        if (lesson == null) return NotFound();
        var access = await GetLessonAccessAsync(lesson);
        if (!access.Allowed) return Forbid();
        if (lesson.Status == LessonStatus.Cancelled) return BadRequest("Cancelled lessons do not take attendance.");
        return Ok(await BuildRosterAsync(lesson, access.IsAdmin));
    }

    [HttpPut("api/lessons/{lessonId:int}/attendance")]
    public async Task<ActionResult<LessonAttendanceDto>> PutLesson(int lessonId, BulkAttendanceDto dto)
    {
        var lesson = await _lessons.GetByIdAsync(lessonId);
        if (lesson == null) return NotFound();
        var access = await GetLessonAccessAsync(lesson);
        if (!access.Allowed) return Forbid();
        if (lesson.Status == LessonStatus.Cancelled) return BadRequest("Cancelled lessons do not take attendance.");
        if (!access.IsAdmin && lesson.Date.AddDays(_editWindowDays) < DateOnly.FromDateTime(DateTime.Today)) return BadRequest("The attendance edit window has closed.");

        var items = dto.Records.ToList();
        if (items.GroupBy(x => x.StudentId).Any(g => g.Count() > 1)) return BadRequest("A student may appear only once.");
        var rosterIds = (await _attendance.GetRosterAsync(lesson.SubjectGroupId, lesson.Date)).Select(s => s.Id).ToHashSet();
        if (items.Any(x => !rosterIds.Contains(x.StudentId))) return BadRequest("Attendance contains a student who was not on the lesson roster.");
        if (items.Count != rosterIds.Count) return BadRequest("Attendance must include one record for every student on the lesson roster.");

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _attendance.UpsertAsync(lessonId, items.Select(x => (x.StudentId, x.Status, x.Note)), userId);
        return Ok(await BuildRosterAsync(lesson, access.IsAdmin));
    }

    [HttpGet("api/students/{studentId:int}/attendance/summary")]
    public async Task<ActionResult<AttendanceSummaryDto>> GetStudentSummary(int studentId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        if (from > to) return BadRequest("from must be on or before to.");
        if (await _students.GetByIdAsync(studentId) == null) return NotFound();
        var user = await CurrentUserAsync();
        var allowed = User.IsInRole("Admin") || user.StudentId == studentId || (user.TeacherId.HasValue && await _attendance.TeacherHasStudentAsync(user.TeacherId.Value, studentId));
        if (!allowed) return Forbid();
        return Ok(await BuildSummaryAsync(studentId, from, to));
    }

    [HttpGet("api/attendance/me")]
    public async Task<ActionResult<AttendanceSummaryDto>> GetMine([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        if (from > to) return BadRequest("from must be on or before to.");
        var user = await CurrentUserAsync();
        if (!user.StudentId.HasValue) return NotFound();
        return Ok(await BuildSummaryAsync(user.StudentId.Value, from, to));
    }

    private async Task<(bool Allowed, bool IsAdmin)> GetLessonAccessAsync(Lesson lesson)
    {
        if (User.IsInRole("Admin")) return (true, true);
        var user = await CurrentUserAsync();
        return (user.TeacherId.HasValue && lesson.TeacherId == user.TeacherId, false);
    }

    private async Task<User> CurrentUserAsync() => (await _users.GetByIdAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)))!;

    private async Task<LessonAttendanceDto> BuildRosterAsync(Lesson lesson, bool isAdmin)
    {
        var roster = await _attendance.GetRosterAsync(lesson.SubjectGroupId, lesson.Date);
        var records = (await _attendance.GetForLessonAsync(lesson.Id)).ToDictionary(r => r.StudentId);
        var deadline = lesson.Date.AddDays(_editWindowDays);
        var canEdit = isAdmin || deadline >= DateOnly.FromDateTime(DateTime.Today);
        return new(lesson.Id, lesson.Date, canEdit, deadline, roster.Select(s =>
        {
            records.TryGetValue(s.Id, out var r);
            return new AttendanceRosterStudentDto(s.Id, s.Name.FirstName, s.Name.LastName, s.Name.Nickname,
                r == null ? null : new(r.Status, r.Note, r.RecordedByUserId, r.RecordedByUser.Email, r.RecordedAt, r.UpdatedAt));
        }));
    }

    private async Task<AttendanceSummaryDto> BuildSummaryAsync(int studentId, DateOnly? from, DateOnly? to)
    {
        var records = await _attendance.GetForStudentAsync(studentId, from, to, DateOnly.FromDateTime(DateTime.Today));
        static bool Present(AttendanceStatus s) => s is AttendanceStatus.Present or AttendanceStatus.Late;
        static bool Excused(AttendanceStatus s) => s is AttendanceStatus.ExcusedAbsence or AttendanceStatus.Sick or AttendanceStatus.ApprovedLeave;
        static decimal Percent(IEnumerable<AttendanceRecord> rs) { var list = rs.ToList(); return list.Count == 0 ? 0 : Math.Round(list.Count(r => Present(r.Status)) * 100m / list.Count, 2); }
        var totals = new AttendanceStatusTotalsDto(records.Count(r => r.Status == AttendanceStatus.Present), records.Count(r => r.Status == AttendanceStatus.Absent), records.Count(r => r.Status == AttendanceStatus.Late), records.Count(r => r.Status == AttendanceStatus.ExcusedAbsence), records.Count(r => r.Status == AttendanceStatus.Sick), records.Count(r => r.Status == AttendanceStatus.ApprovedLeave));
        var groups = records.GroupBy(r => r.Lesson.SubjectGroup).Select(g => new AttendanceGroupSummaryDto(g.Key.Id, g.Key.Name, g.Key.Subject.NameEnglish, g.Key.Subject.NameThai, g.Count(), Percent(g), g.Count(r => Excused(r.Status)), g.Count(r => r.Status == AttendanceStatus.Absent))).OrderBy(g => g.SubjectGroupName);
        var recent = records.Take(20).Select(r => new RecentAttendanceDto(r.LessonId, r.Lesson.Date, r.Lesson.StartTime, r.Lesson.SubjectGroupId, r.Lesson.SubjectGroup.Name, r.Lesson.SubjectGroup.Subject.NameEnglish, r.Lesson.SubjectGroup.Subject.NameThai, r.Status, r.Note));
        return new(records.Count, totals, Percent(records), records.Count(r => Excused(r.Status)), records.Count(r => r.Status == AttendanceStatus.Absent), groups, recent);
    }
}
