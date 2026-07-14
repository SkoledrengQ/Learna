using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Api.DTOs;

namespace Learna.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/subject-groups/{subjectGroupId}/rules")]
public class LessonRulesController : ControllerBase
{
    private readonly ILessonRuleRepository _ruleRepository;
    private readonly ISubjectGroupRepository _subjectGroupRepository;
    private readonly ILessonSchedulingService _schedulingService;

    public LessonRulesController(
        ILessonRuleRepository ruleRepository,
        ISubjectGroupRepository subjectGroupRepository,
        ILessonSchedulingService schedulingService)
    {
        _ruleRepository = ruleRepository;
        _subjectGroupRepository = subjectGroupRepository;
        _schedulingService = schedulingService;
    }

    private static LessonRuleDto ToDto(LessonRule rule) => new(
        rule.Id,
        rule.SubjectGroupId,
        rule.DayOfWeek,
        rule.StartTime,
        rule.EndTime,
        rule.RoomId,
        rule.Room?.Name,
        rule.StartDate,
        rule.EndDate
    );

    private static LessonConflictDto ToConflictDto(LessonConflict c) => new(
        c.Type, c.LessonId, c.SubjectGroupId, c.SubjectGroupName, c.Date, c.StartTime, c.EndTime, c.Detail
    );

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LessonRuleDto>>> GetAll(int subjectGroupId)
    {
        if (await _subjectGroupRepository.GetByIdAsync(subjectGroupId) == null)
        {
            return NotFound();
        }

        var rules = await _ruleRepository.GetBySubjectGroupIdAsync(subjectGroupId);
        return Ok(rules.Select(ToDto));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LessonRuleDto>> Create(int subjectGroupId, CreateLessonRuleDto createDto)
    {
        var result = await _schedulingService.CreateRuleAsync(
            subjectGroupId, createDto.DayOfWeek, createDto.StartTime, createDto.EndTime,
            createDto.RoomId, createDto.StartDate, createDto.EndDate, createDto.Force);

        if (result.ValidationError != null)
        {
            return BadRequest(result.ValidationError);
        }

        if (result.Rule == null)
        {
            return Conflict(new ConflictResponseDto(result.Conflicts.Select(ToConflictDto)));
        }

        var created = await _ruleRepository.GetByIdAsync(result.Rule.Id) ?? result.Rule;
        return CreatedAtAction(nameof(GetAll), new { subjectGroupId }, ToDto(created));
    }

    [HttpPut("{ruleId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LessonRuleDto>> Update(int subjectGroupId, int ruleId, UpdateLessonRuleDto updateDto)
    {
        var existing = await _ruleRepository.GetByIdAsync(ruleId);
        if (existing == null || existing.SubjectGroupId != subjectGroupId)
        {
            return NotFound();
        }

        var result = await _schedulingService.UpdateRuleAsync(
            ruleId, updateDto.DayOfWeek, updateDto.StartTime, updateDto.EndTime,
            updateDto.RoomId, updateDto.StartDate, updateDto.EndDate, updateDto.Force);

        if (result.NotFound)
        {
            return NotFound();
        }

        if (result.ValidationError != null)
        {
            return BadRequest(result.ValidationError);
        }

        if (result.Rule == null)
        {
            return Conflict(new ConflictResponseDto(result.Conflicts.Select(ToConflictDto)));
        }

        var updated = await _ruleRepository.GetByIdAsync(result.Rule.Id) ?? result.Rule;
        return Ok(ToDto(updated));
    }

    [HttpDelete("{ruleId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int subjectGroupId, int ruleId)
    {
        var existing = await _ruleRepository.GetByIdAsync(ruleId);
        if (existing == null || existing.SubjectGroupId != subjectGroupId)
        {
            return NotFound();
        }

        await _schedulingService.DeleteRuleAsync(ruleId);
        return NoContent();
    }
}
