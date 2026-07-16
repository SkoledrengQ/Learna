using System.Security.Claims;
using Learna.Api.DTOs;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learna.Api.Controllers;

// Aggregate "landing page" data for the two roles that need cross-entity counts
// no existing endpoint provides (missing-attendance / awaiting-grading). Student,
// guardian, and the "recent announcements" card for every role are composed
// client-side from existing endpoints (schedule/me, assignments/my, grades/my,
// attendance/me, announcements/my, guardians/me/children/*) - see WO15 task 5.
[Authorize]
[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(
    IUserRepository users,
    ILessonRepository lessons,
    IAssignmentRepository assignments,
    IStudentRepository students,
    ITeacherRepository teachers,
    ISchoolClassRepository schoolClasses,
    ISubjectGroupRepository subjectGroups,
    TimeProvider timeProvider) : ControllerBase
{
    private const int LookbackDays = 7;
    private const int RecentSubmissionsTake = 5;
    private const int MissingAttendanceListTake = 10;

    [HttpGet("teacher")]
    [Authorize(Roles = "Teacher")]
    public async Task<ActionResult<TeacherDashboardDto>> Teacher()
    {
        var user = await CurrentUserAsync();
        if (user.TeacherId is not int teacherId) return Forbid();

        var today = Today;
        var windowLessons = (await lessons.GetAllAsync(today.AddDays(-LookbackDays), today, null, null, teacherId)).ToList();
        var todayLessons = windowLessons.Where(l => l.Date == today).OrderBy(l => l.StartTime).ToList();
        var missingAttendance = windowLessons
            .Where(l => l.Status != LessonStatus.Cancelled && l.AttendanceRecords.Count == 0)
            .OrderBy(l => l.Date).ThenBy(l => l.StartTime).ToList();

        var teacherGroups = await assignments.GetTeacherGroupsAsync(teacherId);
        var ungraded = new List<(Submission Submission, int SubjectGroupId, string SubjectGroupName, string AssignmentTitle)>();
        foreach (var group in teacherGroups)
        {
            var groupAssignments = await assignments.GetForGroupAsync(group.Id);
            foreach (var assignment in groupAssignments)
                foreach (var submission in assignment.Submissions.Where(s => s.Grade == null))
                    ungraded.Add((submission, group.Id, group.Name, assignment.Title));
        }

        var recentTop = ungraded.OrderByDescending(x => x.Submission.SubmittedAt).Take(RecentSubmissionsTake).ToList();
        var recentSubmissions = new List<TeacherRecentSubmissionDto>();
        foreach (var x in recentTop)
        {
            // GetForGroupAsync doesn't eager-load Submission.Student; resolve it directly (only for the few rows shown).
            var student = await students.GetByIdAsync(x.Submission.StudentId);
            recentSubmissions.Add(new TeacherRecentSubmissionDto(
                x.Submission.Id, x.Submission.StudentId, student == null ? "" : LessonsController.ComposeName(student.Name),
                x.SubjectGroupId, x.SubjectGroupName, x.AssignmentTitle, x.Submission.SubmittedAt, x.Submission.IsLate));
        }

        return Ok(new TeacherDashboardDto(
            todayLessons.Select(l => LessonsController.ToDto(l, true)).ToList(),
            missingAttendance.Count,
            missingAttendance.Take(MissingAttendanceListTake).Select(l => LessonsController.ToDto(l, true)).ToList(),
            ungraded.Count,
            recentSubmissions));
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AdminDashboardDto>> Admin()
    {
        var today = Today;
        var windowLessons = (await lessons.GetAllAsync(today.AddDays(-LookbackDays), today, null, null, null)).ToList();

        return Ok(new AdminDashboardDto(
            (await students.GetAllAsync()).Count(),
            (await teachers.GetAllAsync()).Count(),
            (await schoolClasses.GetAllAsync(null)).Count(),
            (await subjectGroups.GetAllAsync(null, null)).Count(),
            windowLessons.Count(l => l.Date == today),
            windowLessons.Count(l => l.Status != LessonStatus.Cancelled && l.AttendanceRecords.Count == 0)));
    }

    private DateOnly Today => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
    private async Task<User> CurrentUserAsync() => (await users.GetByIdAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)))!;
}
