using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Api.DTOs;

namespace Learna.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/lessons")]
public class LessonsController : ControllerBase
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ILessonSchedulingService _schedulingService;

    public LessonsController(ILessonRepository lessonRepository, ILessonSchedulingService schedulingService)
    {
        _lessonRepository = lessonRepository;
        _schedulingService = schedulingService;
    }

    internal static string ComposeName(PersonName name) => $"{name.FirstName} {name.LastName}";

    internal static LessonDto ToDto(Lesson lesson, bool canManageAttendance = false) => new(
        lesson.Id,
        lesson.SubjectGroupId,
        lesson.SubjectGroup.Name,
        lesson.SubjectGroup.Subject.NameEnglish,
        lesson.SubjectGroup.Subject.NameThai,
        lesson.Date,
        lesson.StartTime,
        lesson.EndTime,
        lesson.RoomId,
        lesson.Room?.Name,
        lesson.TeacherId,
        lesson.Teacher != null ? ComposeName(lesson.Teacher.Name) : null,
        lesson.Status,
        lesson.Note,
        lesson.SourceRuleId,
        lesson.IsModified,
        lesson.AttendanceRecords.Count > 0,
        canManageAttendance
    );

    private static LessonConflictDto ToConflictDto(LessonConflict c) => new(
        c.Type, c.LessonId, c.SubjectGroupId, c.SubjectGroupName, c.Date, c.StartTime, c.EndTime, c.Detail
    );

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LessonDto>>> GetAll(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] int? subjectGroupId, [FromQuery] int? roomId, [FromQuery] int? teacherId)
    {
        var lessons = await _lessonRepository.GetAllAsync(from, to, subjectGroupId, roomId, teacherId);
        var canManage = User.IsInRole("Admin");
        return Ok(lessons.Select(l => ToDto(l, canManage)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LessonDto>> Create(CreateLessonDto createDto)
    {
        var result = await _schedulingService.CreateOneOffLessonAsync(
            createDto.SubjectGroupId, createDto.Date, createDto.StartTime, createDto.EndTime,
            createDto.RoomId, createDto.TeacherId, createDto.Note, createDto.Force);

        if (result.ValidationError != null)
        {
            return BadRequest(result.ValidationError);
        }

        if (result.Lesson == null)
        {
            return Conflict(new ConflictResponseDto(result.Conflicts.Select(ToConflictDto)));
        }

        var created = await _lessonRepository.GetByIdAsync(result.Lesson.Id) ?? result.Lesson;
        return CreatedAtAction(nameof(GetAll), null, ToDto(created, true));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LessonDto>> Update(int id, UpdateLessonDto updateDto)
    {
        var result = await _schedulingService.UpdateLessonAsync(
            id, updateDto.Date, updateDto.StartTime, updateDto.EndTime,
            updateDto.RoomId, updateDto.TeacherId, updateDto.Note, updateDto.Status, updateDto.Force);

        if (result.NotFound)
        {
            return NotFound();
        }

        if (result.ValidationError != null)
        {
            return BadRequest(result.ValidationError);
        }

        if (result.Lesson == null)
        {
            return Conflict(new ConflictResponseDto(result.Conflicts.Select(ToConflictDto)));
        }

        var updated = await _lessonRepository.GetByIdAsync(result.Lesson.Id) ?? result.Lesson;
        return Ok(ToDto(updated, true));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var lesson = await _lessonRepository.GetByIdAsync(id);
        if (lesson == null)
        {
            return NotFound();
        }

        if (lesson.SourceRuleId != null)
        {
            return BadRequest("Rule-generated lessons cannot be deleted; cancel them instead.");
        }

        await _lessonRepository.DeleteAsync(id);
        return NoContent();
    }
}
