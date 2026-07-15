using System.Security.Claims;
using Learna.Api.DTOs;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learna.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/announcements")]
public sealed class AnnouncementsController(IAnnouncementRepository announcements, IUserRepository users, IFileStorage storage, TimeProvider timeProvider, ILogger<AnnouncementsController> logger) : ControllerBase
{
    [HttpGet("my")]
    public async Task<ActionResult<AnnouncementFeedDto>> My()
    {
        var user = await CurrentUserAsync();
        var rows = new List<AnnouncementDto>();
        foreach (var announcement in await announcements.GetAllAsync())
            if (await AnnouncementAccess.CanSeeAsync(announcement, user, User.IsInRole("Admin"), announcements, Now))
                rows.Add(await ToDtoAsync(announcement, user));
        return Ok(new AnnouncementFeedDto(rows, rows.Count(a => !a.IsRead)));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AnnouncementDto>> Get(int id)
    {
        var announcement = await announcements.GetByIdAsync(id);
        if (announcement == null) return NotFound();
        var user = await CurrentUserAsync();
        if (!await AnnouncementAccess.CanSeeAsync(announcement, user, User.IsInRole("Admin"), announcements, Now)) return Forbid();
        return Ok(await ToDtoAsync(announcement, user));
    }

    [HttpPost]
    public async Task<ActionResult<AnnouncementDto>> Create(AnnouncementWriteDto dto)
    {
        var user = await CurrentUserAsync();
        if (!await ValidWriteAsync(dto, user)) return BadRequest(new AnnouncementErrorDto("INVALID_ANNOUNCEMENT"));
        if (!await AnnouncementAccess.CanTargetAsync(dto.AudienceType, dto.TargetId, user, User.IsInRole("Admin"), announcements)) return Forbid();
        var now = Now;
        var entity = new Announcement
        {
            Title = dto.Title.Trim(),
            Body = dto.Body.Trim(),
            CreatedByUserId = user.Id,
            CreatedByUser = user,
            AudienceType = dto.AudienceType,
            TargetId = dto.AudienceType == AnnouncementAudienceType.School ? null : dto.TargetId,
            PublishAt = dto.PublishAt ?? now,
            ExpiresAt = dto.ExpiresAt,
            CreatedAt = now
        };
        await announcements.AddAsync(entity);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, await ToDtoAsync(entity, user));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AnnouncementDto>> Update(int id, AnnouncementWriteDto dto)
    {
        var announcement = await announcements.GetByIdAsync(id);
        if (announcement == null) return NotFound();
        var user = await CurrentUserAsync();
        if (!User.IsInRole("Admin") && announcement.CreatedByUserId != user.Id) return Forbid();
        if (!await ValidWriteAsync(dto, user)) return BadRequest(new AnnouncementErrorDto("INVALID_ANNOUNCEMENT"));
        if (!await AnnouncementAccess.CanTargetAsync(dto.AudienceType, dto.TargetId, user, User.IsInRole("Admin"), announcements)) return Forbid();
        announcement.Title = dto.Title.Trim();
        announcement.Body = dto.Body.Trim();
        announcement.AudienceType = dto.AudienceType;
        announcement.TargetId = dto.AudienceType == AnnouncementAudienceType.School ? null : dto.TargetId;
        announcement.PublishAt = dto.PublishAt ?? announcement.PublishAt;
        announcement.ExpiresAt = dto.ExpiresAt;
        announcement.UpdatedAt = Now;
        await announcements.SaveAsync();
        return Ok(await ToDtoAsync(announcement, user));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var announcement = await announcements.GetByIdAsync(id);
        if (announcement == null) return NotFound();
        var user = await CurrentUserAsync();
        if (!User.IsInRole("Admin") && announcement.CreatedByUserId != user.Id) return Forbid();
        foreach (var file in announcement.Files)
            try { await storage.DeleteAsync(file.StoredPath, file.StoredName, cancellationToken); }
            catch (Exception ex) { logger.LogError(ex, "Failed to remove announcement file {FileId}", file.Id); }
        await announcements.DeleteAsync(announcement);
        return NoContent();
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> Read(int id)
    {
        var announcement = await announcements.GetByIdAsync(id);
        if (announcement == null) return NotFound();
        var user = await CurrentUserAsync();
        if (!await AnnouncementAccess.CanSeeAsync(announcement, user, User.IsInRole("Admin"), announcements, Now)) return Forbid();
        await announcements.MarkReadAsync(id, user.Id, Now);
        return NoContent();
    }

    [HttpGet("targets")]
    public async Task<ActionResult<AnnouncementTargetsDto>> Targets()
    {
        var user = await CurrentUserAsync();
        var targets = new List<AnnouncementTargetDto>();
        if (User.IsInRole("Admin")) targets.Add(new AnnouncementTargetDto(AnnouncementAudienceType.School, null, "School"));
        foreach (var c in await announcements.GetClassesAsync())
            if (User.IsInRole("Admin") || user.TeacherId.HasValue && c.HomeroomTeacherId == user.TeacherId)
                targets.Add(new AnnouncementTargetDto(AnnouncementAudienceType.SchoolClass, c.Id, c.Name));
        foreach (var g in await announcements.GetSubjectGroupsAsync())
            if (User.IsInRole("Admin") || user.TeacherId.HasValue && g.TeacherId == user.TeacherId)
                targets.Add(new AnnouncementTargetDto(AnnouncementAudienceType.SubjectGroup, g.Id, g.Name));
        if (targets.Count == 0) return Forbid();
        return Ok(new AnnouncementTargetsDto(targets));
    }

    private async Task<bool> ValidWriteAsync(AnnouncementWriteDto dto, User user)
    {
        await Task.CompletedTask;
        if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Trim().Length > 300 || string.IsNullOrWhiteSpace(dto.Body)) return false;
        if (dto.AudienceType == AnnouncementAudienceType.School && dto.TargetId != null) return false;
        if (dto.AudienceType != AnnouncementAudienceType.School && dto.TargetId == null) return false;
        if (dto.ExpiresAt.HasValue && dto.ExpiresAt <= (dto.PublishAt ?? Now)) return false;
        return User.IsInRole("Admin") || user.TeacherId.HasValue;
    }

    private async Task<AnnouncementDto> ToDtoAsync(Announcement a, User user)
    {
        var isRead = a.Reads.Any(r => r.UserId == user.Id);
        var canEdit = User.IsInRole("Admin") || a.CreatedByUserId == user.Id;
        return new AnnouncementDto(a.Id, a.Title, a.Body, a.CreatedByUserId, a.CreatedByUser.Email, a.AudienceType, a.TargetId, await AudienceLabelAsync(a), a.PublishAt, a.ExpiresAt, a.CreatedAt, a.UpdatedAt, isRead, a.PublishAt > Now, a.ExpiresAt.HasValue && a.ExpiresAt <= Now, canEdit, a.Files.OrderByDescending(f => f.CreatedAt).Select(f => new AnnouncementFileDto(f.Id, f.OriginalFileName, f.ContentType, f.SizeBytes, f.Description, f.CreatedAt)).ToList());
    }

    private async Task<string> AudienceLabelAsync(Announcement a)
    {
        if (a.AudienceType == AnnouncementAudienceType.School) return "School";
        if (!a.TargetId.HasValue) return "";
        return a.AudienceType == AnnouncementAudienceType.SchoolClass
            ? (await announcements.GetClassAsync(a.TargetId.Value))?.Name ?? ""
            : (await announcements.GetSubjectGroupAsync(a.TargetId.Value))?.Name ?? "";
    }

    private async Task<User> CurrentUserAsync() => (await users.GetByIdAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)))!;
    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;
}
