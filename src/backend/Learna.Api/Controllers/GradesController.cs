using System.Security.Claims;
using Learna.Api.DTOs;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Learna.Api.Controllers;

[Authorize]
[ApiController]
public sealed class GradesController(IGradeRepository grades, IUserRepository users) : ControllerBase
{
    [HttpPut("api/submissions/{submissionId:int}/grade")]
    public async Task<ActionResult<GradeDto>> GradeSubmission(int submissionId, GradeWriteDto dto)
    {
        var submission = await grades.GetSubmissionAsync(submissionId); if (submission == null) return NotFound();
        var user = await CurrentUserAsync(); if (!CanManage(user, submission.Assignment.SubjectGroup)) return Forbid();
        if (!Valid(dto.Score, dto.MaxScore) || !await grades.IsEnrolledAsync(submission.Assignment.SubjectGroupId, submission.StudentId)) return BadRequest(new GradeErrorDto("INVALID_GRADE"));
        var grade = await grades.GetForSubmissionAsync(submissionId);
        if (grade == null)
        {
            grade = New(submission.StudentId, submission.Assignment.SubjectGroupId, user.Id, dto.Category ?? submission.Assignment.Title, dto.Score, dto.MaxScore, dto.Feedback);
            grade.SubmissionId = submissionId;
            try { await grades.AddAsync(grade); } catch (DbUpdateException) { return Conflict(new GradeErrorDto("GRADE_ALREADY_EXISTS")); }
            grade = await grades.GetAsync(grade.Id);
        }
        else
        {
            Apply(grade, dto.Score, dto.MaxScore, dto.Feedback, dto.Category ?? submission.Assignment.Title);
            // TODO: add a grade-change audit trail when the audit work order is implemented.
            await grades.SaveAsync();
        }
        return Ok(ToDto(grade!));
    }

    [HttpPost("api/assignments/{assignmentId:int}/grades/publish")]
    public async Task<ActionResult<IEnumerable<GradeDto>>> PublishAssignment(int assignmentId)
    {
        var assignment = await grades.GetAssignmentAsync(assignmentId); if (assignment == null) return NotFound();
        var rows = await grades.GetForAssignmentAsync(assignmentId); var user = await CurrentUserAsync(); if (!CanManage(user, assignment.SubjectGroup)) return Forbid();
        foreach (var grade in rows.Where(g => g.Status == GradeStatus.Draft)) Publish(grade);
        await CompleteAssignmentIfReady(assignment, rows); await grades.SaveAsync();
        return Ok(rows.Select(ToDto));
    }

    [HttpGet("api/subject-groups/{groupId:int}/grades")]
    public async Task<ActionResult<IEnumerable<GradeDto>>> GroupGrades(int groupId)
    {
        var group = await grades.GetGroupAsync(groupId); if (group == null) return NotFound(); var user = await CurrentUserAsync();
        if (!CanManage(user, group)) return Forbid(); return Ok((await grades.GetForGroupAsync(groupId)).Select(ToDto));
    }

    [HttpPost("api/subject-groups/{groupId:int}/grades")]
    public async Task<ActionResult<GradeDto>> CreateManual(int groupId, ManualGradeWriteDto dto)
    {
        var group = await grades.GetGroupAsync(groupId); if (group == null) return NotFound(); var user = await CurrentUserAsync();
        if (!CanManage(user, group)) return Forbid();
        if (!Valid(dto.Score, dto.MaxScore) || string.IsNullOrWhiteSpace(dto.Category) || !await grades.IsEnrolledAsync(groupId, dto.StudentId)) return BadRequest(new GradeErrorDto("INVALID_GRADE"));
        var grade = New(dto.StudentId, groupId, user.Id, dto.Category, dto.Score, dto.MaxScore, dto.Feedback); await grades.AddAsync(grade);
        return CreatedAtAction(nameof(GroupGrades), new { groupId }, ToDto((await grades.GetAsync(grade.Id))!));
    }

    [HttpPut("api/grades/{id:int}")]
    public async Task<ActionResult<GradeDto>> Update(int id, GradeWriteDto dto)
    {
        var grade = await grades.GetAsync(id); if (grade == null) return NotFound(); var user = await CurrentUserAsync();
        if (!CanManage(user, grade.SubjectGroup)) return Forbid(); if (!Valid(dto.Score, dto.MaxScore)) return BadRequest(new GradeErrorDto("INVALID_GRADE"));
        Apply(grade, dto.Score, dto.MaxScore, dto.Feedback, dto.Category ?? grade.Category);
        // TODO: add a grade-change audit trail when the audit work order is implemented.
        await grades.SaveAsync(); return Ok(ToDto(grade));
    }

    [HttpDelete("api/grades/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var grade = await grades.GetAsync(id); if (grade == null) return NotFound(); var user = await CurrentUserAsync();
        if (!CanManage(user, grade.SubjectGroup)) return Forbid(); if (grade.Status == GradeStatus.Published) return Conflict(new GradeErrorDto("PUBLISHED_GRADE_DELETE_BLOCKED"));
        await grades.DeleteAsync(grade); return NoContent();
    }

    [HttpPost("api/grades/{id:int}/publish")]
    public async Task<ActionResult<GradeDto>> PublishOne(int id)
    {
        var grade = await grades.GetAsync(id); if (grade == null) return NotFound(); var user = await CurrentUserAsync();
        if (!CanManage(user, grade.SubjectGroup)) return Forbid(); Publish(grade);
        if (grade.Submission?.Assignment is { } assignment) await CompleteAssignmentIfReady(assignment, await grades.GetForAssignmentAsync(assignment.Id));
        await grades.SaveAsync(); return Ok(ToDto(grade));
    }

    [HttpGet("api/grades/my")]
    public async Task<ActionResult<IEnumerable<GradeGroupDto>>> Mine()
    {
        var user = await CurrentUserAsync(); if (user.StudentId is not int studentId) return Forbid();
        return Ok(Group(await grades.GetPublishedForStudentAsync(studentId)));
    }

    [HttpGet("api/guardians/me/children/{studentId:int}/grades")]
    public async Task<ActionResult<IEnumerable<GradeGroupDto>>> GuardianGrades(int studentId)
    {
        var user = await CurrentUserAsync(); if (user.GuardianId is not int guardianId) return Forbid();
        if (!await grades.GuardianOwnsStudentAsync(guardianId, studentId)) return Forbid();
        return Ok(Group(await grades.GetPublishedForStudentAsync(studentId)));
    }

    private async Task CompleteAssignmentIfReady(Assignment assignment, IReadOnlyList<Grade> assignmentGrades)
    {
        var submissions = assignment.Submissions.Count;
        if (submissions > 0 && assignmentGrades.Count == submissions && assignmentGrades.All(g => g.Status == GradeStatus.Published)) assignment.Status = AssignmentStatus.Graded;
        await Task.CompletedTask;
    }
    private static IEnumerable<GradeGroupDto> Group(IReadOnlyList<Grade> rows) => rows.GroupBy(g => g.SubjectGroupId).Select(g => new GradeGroupDto(g.Key, g.First().SubjectGroup.Name, g.First().SubjectGroup.Subject.NameEnglish, Math.Round(g.Average(x => x.Score / x.MaxScore * 100), 2), g.Select(ToDto).ToList()));
    private static Grade New(int studentId, int groupId, int userId, string? category, decimal score, decimal max, string? feedback) => new() { StudentId = studentId, SubjectGroupId = groupId, GradedByUserId = userId, Category = category?.Trim() ?? "", Score = score, MaxScore = max, Feedback = Clean(feedback), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
    private static void Apply(Grade grade, decimal score, decimal max, string? feedback, string category) { grade.Score = score; grade.MaxScore = max; grade.Feedback = Clean(feedback); grade.Category = category.Trim(); grade.UpdatedAt = DateTime.UtcNow; }
    private static void Publish(Grade grade) { if (grade.Status == GradeStatus.Draft) { grade.Status = GradeStatus.Published; grade.PublishedAt = DateTime.UtcNow; grade.UpdatedAt = DateTime.UtcNow; } }
    private static bool Valid(decimal score, decimal max) => max > 0 && score >= 0 && score <= max;
    private bool CanManage(User user, SubjectGroup group) => User.IsInRole("Admin") || user.TeacherId.HasValue && user.TeacherId == group.TeacherId;
    private async Task<User> CurrentUserAsync() => (await users.GetByIdAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)))!;
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public static GradeDto ToDto(Grade g) => new(g.Id, g.StudentId, $"{g.Student.Name.FirstName} {g.Student.Name.LastName}", g.SubjectGroupId, g.SubmissionId, g.Submission?.AssignmentId, g.Category, g.Score, g.MaxScore, g.Feedback, g.Status, g.PublishedAt, g.CreatedAt, g.UpdatedAt);
}
