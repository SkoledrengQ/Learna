using System.Security.Claims;
using Learna.Api.DTOs;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learna.Api.Controllers;

[Authorize(Roles = "Parent")]
[ApiController]
[Route("api/guardians")]
public class GuardiansController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IGuardianRepository _guardians;
    private readonly IAttendanceRepository _attendance;

    public GuardiansController(IUserRepository users, IGuardianRepository guardians, IAttendanceRepository attendance)
    {
        _users = users;
        _guardians = guardians;
        _attendance = attendance;
    }

    [HttpGet("me/children")]
    public async Task<ActionResult<IEnumerable<GuardianChildDto>>> GetMyChildren()
    {
        var user = await CurrentUserAsync();
        if (!user.GuardianId.HasValue) return NotFound();

        var links = await _guardians.GetChildrenAsync(user.GuardianId.Value);
        return Ok(links.Select(ToChildDto));
    }

    [HttpGet("me/children/{studentId:int}/attendance")]
    public async Task<ActionResult<AttendanceSummaryDto>> GetChildAttendance(
        int studentId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        if (from > to) return BadRequest("from must be on or before to.");
        var user = await CurrentUserAsync();
        if (!user.GuardianId.HasValue) return NotFound();
        if (await _guardians.GetLinkAsync(studentId, user.GuardianId.Value) == null) return Forbid();

        return Ok(await AttendanceController.BuildSummaryAsync(_attendance, studentId, from, to));
    }

    private async Task<User> CurrentUserAsync() =>
        (await _users.GetByIdAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)))!;

    private static GuardianChildDto ToChildDto(StudentGuardian link)
    {
        var student = link.Student;
        var activeClass = student.ClassMemberships
            .Where(m => m.LeftDate == null)
            .OrderByDescending(m => m.JoinedDate)
            .Select(m => m.Class.Name)
            .FirstOrDefault();

        return new GuardianChildDto(
            student.Id,
            new PersonNameDto(student.Name.Title, student.Name.FirstName, student.Name.LastName,
                student.Name.FirstNameEnglish, student.Name.LastNameEnglish, student.Name.Nickname),
            link.Relationship,
            activeClass);
    }
}
