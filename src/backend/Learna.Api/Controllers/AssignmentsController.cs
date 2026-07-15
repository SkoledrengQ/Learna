using System.Security.Claims;
using Learna.Api.DTOs;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learna.Api.Controllers;

[Authorize]
[ApiController]
public sealed class AssignmentsController(IAssignmentRepository assignments, ISubjectGroupRepository groups,
    IUserRepository users, IFileStorage storage, TimeProvider timeProvider, ILogger<AssignmentsController> logger) : ControllerBase
{
    [HttpGet("api/subject-groups/{groupId:int}/assignments")]
    public async Task<ActionResult<IEnumerable<AssignmentDto>>> GroupAssignments(int groupId)
    {
        var group = await groups.GetByIdAsync(groupId); if (group == null) return NotFound();
        var user = await CurrentUserAsync();
        if (!CanManage(user, group)) return Forbid();
        var roster = await assignments.GetRosterAsync(groupId);
        return Ok((await assignments.GetForGroupAsync(groupId)).Select(a => ToDto(a, null, roster.Count)));
    }

    [HttpPost("api/subject-groups/{groupId:int}/assignments")]
    public async Task<ActionResult<AssignmentDto>> Create(int groupId, AssignmentWriteDto dto)
    {
        var group = await groups.GetByIdAsync(groupId); if (group == null) return NotFound();
        var user = await CurrentUserAsync(); if (!CanManage(user, group)) return Forbid();
        if (!Valid(dto)) return BadRequest(new AssignmentErrorDto("INVALID_ASSIGNMENT"));
        var entity = new Assignment { SubjectGroupId = groupId, SubjectGroup = group, Title = dto.Title.Trim(), Description = Clean(dto.Description), StartDate = dto.StartDate, DeadlineDate = dto.DeadlineDate, DeadlineTime = dto.DeadlineTime, LatePolicy = dto.LatePolicy, CreatedByUserId = user.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await assignments.AddAsync(entity);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, ToDto(entity));
    }

    [HttpGet("api/assignments/{id:int}")]
    public async Task<ActionResult<AssignmentDto>> Get(int id)
    {
        var a = await assignments.GetAsync(id); if (a == null) return NotFound();
        var user = await CurrentUserAsync();
        if (CanManage(user, a.SubjectGroup)) return Ok(ToDto(a, user.StudentId));
        if (user.StudentId is int studentId)
        {
            if (!await assignments.IsEnrolledAsync(a.SubjectGroupId, studentId)) return Forbid();
            if (!Visible(a)) return NotFound();
            return Ok(ToDto(a, studentId));
        }
        return Forbid();
    }

    [HttpPut("api/assignments/{id:int}")]
    public async Task<ActionResult<AssignmentDto>> Update(int id, AssignmentWriteDto dto)
    {
        var a = await assignments.GetAsync(id); if (a == null) return NotFound();
        var user = await CurrentUserAsync(); if (!CanManage(user, a.SubjectGroup)) return Forbid();
        if (!Valid(dto) || a.Status is AssignmentStatus.Graded or AssignmentStatus.Archived) return BadRequest(new AssignmentErrorDto("INVALID_ASSIGNMENT"));
        a.Title = dto.Title.Trim(); a.Description = Clean(dto.Description); a.StartDate = dto.StartDate; a.DeadlineDate = dto.DeadlineDate; a.DeadlineTime = dto.DeadlineTime; a.LatePolicy = dto.LatePolicy; a.UpdatedAt = DateTime.UtcNow;
        await assignments.SaveAsync(); return Ok(ToDto(a));
    }

    [HttpDelete("api/assignments/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var a = await assignments.GetAsync(id); if (a == null) return NotFound();
        var user = await CurrentUserAsync(); if (!CanManage(user, a.SubjectGroup)) return Forbid();
        if (a.Status != AssignmentStatus.Draft) return Conflict(new AssignmentErrorDto("DRAFT_ONLY_DELETE"));
        foreach (var file in await assignments.GetAssignmentFilesAsync(id))
            try { await storage.DeleteAsync(file.StoredPath, file.StoredName, cancellationToken); } catch (Exception ex) { logger.LogError(ex, "Failed to remove assignment file {FileId}", file.Id); }
        await assignments.DeleteAsync(a); return NoContent();
    }

    [HttpPost("api/assignments/{id:int}/publish")]
    public Task<ActionResult<AssignmentDto>> Publish(int id) => ChangeStatus(id, AssignmentStatus.Published);
    [HttpPost("api/assignments/{id:int}/close")]
    public Task<ActionResult<AssignmentDto>> Close(int id) => ChangeStatus(id, AssignmentStatus.Closed);

    [HttpPut("api/assignments/{id:int}/extensions/{studentId:int}")]
    public async Task<ActionResult<ExtensionDto>> PutExtension(int id, int studentId, ExtensionWriteDto dto)
    {
        var a = await assignments.GetAsync(id); if (a == null) return NotFound(); var user = await CurrentUserAsync();
        if (!CanManage(user, a.SubjectGroup)) return Forbid(); if (!await assignments.IsEnrolledAsync(a.SubjectGroupId, studentId)) return BadRequest(new AssignmentErrorDto("STUDENT_NOT_ENROLLED"));
        var e = await assignments.GetExtensionAsync(id, studentId) ?? new AssignmentExtension { AssignmentId = id, StudentId = studentId };
        e.ExtendedDeadlineDate = dto.ExtendedDeadlineDate; e.ExtendedDeadlineTime = dto.ExtendedDeadlineTime; e.Note = Clean(dto.Note); await assignments.UpsertExtensionAsync(e);
        return Ok(new ExtensionDto(studentId, e.ExtendedDeadlineDate, e.ExtendedDeadlineTime, e.Note));
    }

    [HttpDelete("api/assignments/{id:int}/extensions/{studentId:int}")]
    public async Task<IActionResult> DeleteExtension(int id, int studentId)
    {
        var a = await assignments.GetAsync(id); if (a == null) return NotFound(); var user = await CurrentUserAsync(); if (!CanManage(user, a.SubjectGroup)) return Forbid();
        var e = await assignments.GetExtensionAsync(id, studentId); if (e == null) return NotFound(); await assignments.DeleteExtensionAsync(e); return NoContent();
    }

    [HttpGet("api/assignments/{id:int}/submissions")]
    public async Task<ActionResult<IEnumerable<SubmissionRosterDto>>> Submissions(int id)
    {
        var a = await assignments.GetAsync(id); if (a == null) return NotFound(); var user = await CurrentUserAsync(); if (!CanManage(user, a.SubjectGroup)) return Forbid();
        var roster = await assignments.GetRosterAsync(a.SubjectGroupId); var submissions = (await assignments.GetSubmissionsAsync(id)).ToDictionary(s => s.StudentId);
        return Ok(roster.Select(e => { submissions.TryGetValue(e.StudentId, out var s); var ext = a.Extensions.FirstOrDefault(x => x.StudentId == e.StudentId); var status = s == null ? "Missing" : s.IsLate ? "Late" : "Submitted"; return new SubmissionRosterDto(e.StudentId, $"{e.Student.Name.FirstName} {e.Student.Name.LastName}", e.Student.StudentId, status, s == null ? null : ToSubmission(s), ext == null ? null : new ExtensionDto(e.StudentId, ext.ExtendedDeadlineDate, ext.ExtendedDeadlineTime, ext.Note)); }));
    }

    [HttpPost("api/assignments/{id:int}/submission")]
    [RequestSizeLimit(262_144_000)]
    public async Task<ActionResult<SubmissionDto>> Submit(int id, [FromForm] List<IFormFile>? files, [FromForm] string? text, CancellationToken cancellationToken)
    {
        var a = await assignments.GetAsync(id); if (a == null) return NotFound(); var user = await CurrentUserAsync(); if (user.StudentId is not int studentId || !await assignments.IsEnrolledAsync(a.SubjectGroupId, studentId)) return Forbid();
        if (!Visible(a)) return Forbid(); if (a.Status == AssignmentStatus.Closed) return Conflict(new AssignmentErrorDto("ASSIGNMENT_CLOSED"));
        var deadline = EffectiveDeadline(a, studentId); var late = Now > deadline;
        if (late && a.LatePolicy == LatePolicy.Block) return Conflict(new AssignmentErrorDto("LATE_SUBMISSION_BLOCKED"));
        var existing = await assignments.GetSubmissionAsync(id, studentId); var incoming = files ?? [];
        if (existing == null && incoming.Count == 0 && string.IsNullOrWhiteSpace(text)) return BadRequest(new AssignmentErrorDto("SUBMISSION_EMPTY"));
        var stored = new List<(StoredFile Data, IFormFile Upload)>();
        try { foreach (var upload in incoming) { await using var stream = upload.OpenReadStream(); stored.Add((await storage.StoreAsync(stream, SafeName(upload.FileName), upload.Length, cancellationToken), upload)); } }
        catch (FileStorageValidationException ex) { foreach (var item in stored) await storage.DeleteAsync(item.Data.StoredPath, item.Data.StoredName, cancellationToken); return BadRequest(new FileErrorDto(ex.ErrorCode)); }
        if (existing == null) { existing = new Submission { AssignmentId = id, StudentId = studentId }; a.Submissions.Add(existing); }
        else { foreach (var old in existing.Files) try { await storage.DeleteAsync(old.StoredPath, old.StoredName, cancellationToken); } catch (Exception ex) { logger.LogError(ex, "Failed to remove resubmitted file {FileId}", old.Id); } await assignments.RemoveSubmissionFilesAsync(existing.Id); }
        existing.Text = Clean(text); existing.IsLate = late; existing.SubmittedAt = DateTime.UtcNow; await assignments.SaveAsync();
        foreach (var item in stored) await assignments.AddFileAsync(new FileResource { OriginalFileName = SafeName(item.Upload.FileName), StoredPath = item.Data.StoredPath, StoredName = item.Data.StoredName, ContentType = string.IsNullOrWhiteSpace(item.Upload.ContentType) ? "application/octet-stream" : item.Upload.ContentType, SizeBytes = item.Data.SizeBytes, UploadedByUserId = user.Id, CreatedAt = DateTime.UtcNow, SubmissionId = existing.Id });
        existing = await assignments.GetSubmissionAsync(id, studentId); return Ok(ToSubmission(existing!));
    }

    [HttpGet("api/assignments/my")]
    public async Task<ActionResult<IEnumerable<AssignmentDto>>> Mine()
    {
        var user = await CurrentUserAsync(); if (user.StudentId is not int studentId) return Forbid();
        return Ok((await assignments.GetForStudentAsync(studentId)).Where(Visible).Select(a => ToDto(a, studentId)));
    }

    [HttpGet("api/guardians/me/children/{studentId:int}/assignments")]
    public async Task<ActionResult<IEnumerable<GuardianAssignmentDto>>> GuardianAssignments(int studentId)
    {
        var user = await CurrentUserAsync(); if (user.GuardianId is not int guardianId) return Forbid();
        var rows = await assignments.GetForGuardianChildAsync(guardianId, studentId);
        if (!await assignments.GuardianOwnsStudentAsync(guardianId, studentId)) return Forbid();
        return Ok(rows.Where(Visible).Select(a => { var d = EffectiveDeadline(a, studentId); var s = a.Submissions.FirstOrDefault(x => x.StudentId == studentId); return new GuardianAssignmentDto(a.Id, a.Title, a.SubjectGroupId, a.SubjectGroup.Name, DateOnly.FromDateTime(d), TimeOnly.FromDateTime(d), a.Extensions.Any(e => e.StudentId == studentId), s == null ? "Missing" : s.IsLate ? "Late" : "Submitted", Now > d); }));
    }

    private async Task<ActionResult<AssignmentDto>> ChangeStatus(int id, AssignmentStatus status) { var a = await assignments.GetAsync(id); if (a == null) return NotFound(); var user = await CurrentUserAsync(); if (!CanManage(user, a.SubjectGroup)) return Forbid(); if (status == AssignmentStatus.Published && a.Status != AssignmentStatus.Draft || status == AssignmentStatus.Closed && a.Status != AssignmentStatus.Published) return Conflict(new AssignmentErrorDto("INVALID_STATUS_TRANSITION")); a.Status = status; a.UpdatedAt = DateTime.UtcNow; await assignments.SaveAsync(); return Ok(ToDto(a)); }
    private static bool Valid(AssignmentWriteDto d) => !string.IsNullOrWhiteSpace(d.Title) && d.Title.Trim().Length <= 300 && (!d.StartDate.HasValue || d.StartDate <= d.DeadlineDate);
    private bool CanManage(User u, SubjectGroup g) => User.IsInRole("Admin") || u.TeacherId.HasValue && u.TeacherId == g.TeacherId;
    private bool Visible(Assignment a) => a.Status != AssignmentStatus.Draft && a.Status != AssignmentStatus.Archived && (!a.StartDate.HasValue || a.StartDate <= DateOnly.FromDateTime(Now));
    private static DateTime EffectiveDeadline(Assignment a, int studentId) { var e = a.Extensions.FirstOrDefault(x => x.StudentId == studentId); return (e == null ? a.DeadlineDate.ToDateTime(a.DeadlineTime) : e.ExtendedDeadlineDate.ToDateTime(e.ExtendedDeadlineTime)); }
    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static string SafeName(string name) => name.Replace('\\', '/').Split('/').Last();
    private static AssignmentFileDto FileDto(FileResource f) => new(f.Id, f.OriginalFileName, f.ContentType, f.SizeBytes, f.Description, f.CreatedAt);
    private static SubmissionDto ToSubmission(Submission s) => new(s.Id, s.StudentId, s.SubmittedAt, s.Text, s.IsLate, s.Files.Select(FileDto).ToList());
    private AssignmentDto ToDto(Assignment a, int? studentId = null, int? rosterCount = null) { var effective = studentId.HasValue ? EffectiveDeadline(a, studentId.Value) : a.DeadlineDate.ToDateTime(a.DeadlineTime); var own = studentId.HasValue ? a.Submissions.FirstOrDefault(s => s.StudentId == studentId) : null; return new(a.Id, a.SubjectGroupId, a.SubjectGroup.Name, a.Title, a.Description, a.StartDate, a.DeadlineDate, a.DeadlineTime, DateOnly.FromDateTime(effective), TimeOnly.FromDateTime(effective), studentId.HasValue && a.Extensions.Any(e => e.StudentId == studentId), a.LatePolicy, a.Status, a.Submissions.Count, rosterCount ?? 0, own == null ? null : ToSubmission(own), a.Files.Select(FileDto).ToList(), a.CreatedAt, a.UpdatedAt); }
    private async Task<User> CurrentUserAsync() => (await users.GetByIdAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)))!;
    private DateTime Now => timeProvider.GetLocalNow().DateTime;
}
