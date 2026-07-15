using System.Net.Http.Headers;
using System.Security.Claims;
using Learna.Api.DTOs;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Learna.Api.Controllers;

[Authorize]
[ApiController]
public sealed class FilesController(IFileResourceRepository files, IFileStorage storage, IUserRepository users, ILogger<FilesController> logger) : ControllerBase
{
    [HttpPost("api/subject-groups/{id:int}/files")]
    [RequestSizeLimit(262_144_000)]
    public async Task<ActionResult<FileResourceDto>> UploadToSubjectGroup(int id, IFormFile file, [FromForm] string? description, CancellationToken cancellationToken)
    {
        var group = await files.GetSubjectGroupAsync(id);
        if (group == null) return NotFound();
        var user = await CurrentUserAsync();
        if (!User.IsInRole("Admin") && (!user.TeacherId.HasValue || user.TeacherId != group.TeacherId)) return Forbid();
        return await StoreAsync(file, description, user, group, null, cancellationToken);
    }

    [HttpPost("api/lessons/{id:int}/files")]
    [RequestSizeLimit(262_144_000)]
    public async Task<ActionResult<FileResourceDto>> UploadToLesson(int id, IFormFile file, [FromForm] string? description, CancellationToken cancellationToken)
    {
        var lesson = await files.GetLessonAsync(id);
        if (lesson == null) return NotFound();
        var user = await CurrentUserAsync();
        if (!User.IsInRole("Admin") && (!user.TeacherId.HasValue || user.TeacherId != lesson.TeacherId)) return Forbid();
        return await StoreAsync(file, description, user, null, lesson, cancellationToken);
    }

    [HttpGet("api/subject-groups/{id:int}/files")]
    public async Task<ActionResult<IEnumerable<FileResourceDto>>> ListSubjectGroup(int id)
    {
        var group = await files.GetSubjectGroupAsync(id);
        if (group == null) return NotFound();
        var user = await CurrentUserAsync();
        if (!await CanReadGroupAsync(user, group.Id, group.TeacherId)) return Forbid();
        return Ok((await files.GetForSubjectGroupAsync(id)).Select(f => ToDto(f, user)));
    }

    [HttpGet("api/lessons/{id:int}/files")]
    public async Task<ActionResult<IEnumerable<FileResourceDto>>> ListLesson(int id)
    {
        var lesson = await files.GetLessonAsync(id);
        if (lesson == null) return NotFound();
        var user = await CurrentUserAsync();
        if (!await CanReadGroupAsync(user, lesson.SubjectGroupId, lesson.SubjectGroup.TeacherId) && (!user.TeacherId.HasValue || user.TeacherId != lesson.TeacherId)) return Forbid();
        return Ok((await files.GetForLessonAsync(id)).Select(f => ToDto(f, user)));
    }

    [HttpGet("api/files/{id:int}/download")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var file = await files.GetByIdAsync(id);
        if (file == null) return NotFound();
        var user = await CurrentUserAsync();
        var groupId = file.SubjectGroupId ?? file.Lesson!.SubjectGroupId;
        var groupTeacherId = file.SubjectGroup?.TeacherId ?? file.Lesson!.SubjectGroup.TeacherId;
        if (!await CanReadGroupAsync(user, groupId, groupTeacherId) && (!user.TeacherId.HasValue || user.TeacherId != file.Lesson?.TeacherId)) return Forbid();
        var stream = await storage.OpenReadAsync(file.StoredPath, file.StoredName, cancellationToken);
        if (stream == null) return NotFound();
        var disposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") { FileName = "download", FileNameStar = file.OriginalFileName };
        Response.Headers[HeaderNames.ContentDisposition] = disposition.ToString();
        return new FileStreamResult(stream, file.ContentType) { EnableRangeProcessing = true };
    }

    [HttpDelete("api/files/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var file = await files.GetByIdAsync(id);
        if (file == null) return NotFound();
        var user = await CurrentUserAsync();
        var targetTeacherId = file.LessonId.HasValue ? file.Lesson?.TeacherId : file.SubjectGroup?.TeacherId;
        if (!User.IsInRole("Admin") && user.Id != file.UploadedByUserId && (!user.TeacherId.HasValue || user.TeacherId != targetTeacherId)) return Forbid();
        await files.DeleteAsync(file);
        try { await storage.DeleteAsync(file.StoredPath, file.StoredName, cancellationToken); }
        catch (Exception ex) { logger.LogError(ex, "Database file resource {FileId} was deleted, but disk cleanup failed for {StoredPath}/{StoredName}", file.Id, file.StoredPath, file.StoredName); }
        return NoContent();
    }

    [HttpGet("api/files/my")]
    public async Task<ActionResult<IEnumerable<FileResourceDto>>> Mine()
    {
        var user = await CurrentUserAsync();
        var visible = await files.GetVisibleForUserAsync(user, User.IsInRole("Admin"));
        return Ok(visible.Select(x => ToDto(x.File, user, x.ChildId, x.ChildName)));
    }

    private async Task<ActionResult<FileResourceDto>> StoreAsync(IFormFile upload, string? description, User user, SubjectGroup? group, Lesson? lesson, CancellationToken cancellationToken)
    {
        var originalName = upload.FileName.Replace('\\', '/').Split('/').Last();
        StoredFile stored;
        try { await using var stream = upload.OpenReadStream(); stored = await storage.StoreAsync(stream, originalName, upload.Length, cancellationToken); }
        catch (FileStorageValidationException ex) { return BadRequest(new FileErrorDto(ex.ErrorCode)); }
        var entity = new FileResource { OriginalFileName = originalName, StoredPath = stored.StoredPath, StoredName = stored.StoredName, ContentType = string.IsNullOrWhiteSpace(upload.ContentType) ? "application/octet-stream" : upload.ContentType, SizeBytes = stored.SizeBytes, Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(), UploadedByUserId = user.Id, CreatedAt = DateTime.UtcNow, SubjectGroupId = group?.Id, LessonId = lesson?.Id };
        try { await files.AddAsync(entity); }
        catch { await storage.DeleteAsync(stored.StoredPath, stored.StoredName, cancellationToken); throw; }
        entity.UploadedByUser = user; entity.SubjectGroup = group; entity.Lesson = lesson;
        return Created($"/api/files/{entity.Id}/download", ToDto(entity, user));
    }

    private async Task<bool> CanReadGroupAsync(User user, int groupId, int? groupTeacherId) => User.IsInRole("Admin") || (user.TeacherId.HasValue && user.TeacherId == groupTeacherId) || (user.StudentId.HasValue && await files.HasActiveEnrollmentAsync(user.StudentId.Value, groupId)) || (user.GuardianId.HasValue && await files.GuardianHasActiveEnrollmentAsync(user.GuardianId.Value, groupId));
    private bool CanDelete(FileResource file, User user) => User.IsInRole("Admin") || user.Id == file.UploadedByUserId || (user.TeacherId.HasValue && user.TeacherId == (file.LessonId.HasValue ? file.Lesson?.TeacherId : file.SubjectGroup?.TeacherId));
    private FileResourceDto ToDto(FileResource f, User user, int? childId = null, string? childName = null) => new(f.Id, f.OriginalFileName, f.ContentType, f.SizeBytes, f.Description, f.UploadedByUserId, f.UploadedByUser.Email, f.CreatedAt, f.SubjectGroupId ?? f.Lesson!.SubjectGroupId, f.SubjectGroup?.Name ?? f.Lesson!.SubjectGroup.Name, f.LessonId, f.Lesson?.Date, CanDelete(f, user), childId, childName);
    private async Task<User> CurrentUserAsync() => (await users.GetByIdAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)))!;
}
