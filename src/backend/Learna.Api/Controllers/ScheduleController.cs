using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Api.DTOs;

namespace Learna.Api.Controllers;

// Read-only schedule views, consumed by student/teacher/room-facing screens (the
// calendar UI itself is the next work order). A student's schedule is derived from
// their ACTIVE enrollments, bounded below by EnrolledDate - see WO5 task 7.
[Authorize]
[ApiController]
[Route("api/schedule")]
public class ScheduleController : ControllerBase
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IUserRepository _userRepository;

    public ScheduleController(
        ILessonRepository lessonRepository,
        IEnrollmentRepository enrollmentRepository,
        IStudentRepository studentRepository,
        ITeacherRepository teacherRepository,
        IRoomRepository roomRepository,
        IUserRepository userRepository)
    {
        _lessonRepository = lessonRepository;
        _enrollmentRepository = enrollmentRepository;
        _studentRepository = studentRepository;
        _teacherRepository = teacherRepository;
        _roomRepository = roomRepository;
        _userRepository = userRepository;
    }

    [HttpGet("students/{id}")]
    public async Task<ActionResult<IEnumerable<LessonDto>>> GetStudentSchedule(int id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        if (await _studentRepository.GetByIdAsync(id) == null)
        {
            return NotFound();
        }

        var lessons = await GetStudentLessonsAsync(id, from, to);
        return Ok(lessons.Select(LessonsController.ToDto));
    }

    [HttpGet("teachers/{id}")]
    public async Task<ActionResult<IEnumerable<LessonDto>>> GetTeacherSchedule(int id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        if (await _teacherRepository.GetByIdAsync(id) == null)
        {
            return NotFound();
        }

        var lessons = await _lessonRepository.GetAllAsync(from, to, null, null, id);
        return Ok(lessons.Select(LessonsController.ToDto));
    }

    [HttpGet("rooms/{id}")]
    public async Task<ActionResult<IEnumerable<LessonDto>>> GetRoomSchedule(int id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        if (await _roomRepository.GetByIdAsync(id) == null)
        {
            return NotFound();
        }

        var lessons = await _lessonRepository.GetAllAsync(from, to, null, id, null);
        return Ok(lessons.Select(LessonsController.ToDto));
    }

    [HttpGet("me")]
    public async Task<ActionResult<IEnumerable<LessonDto>>> GetMySchedule([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        if (user.StudentId.HasValue)
        {
            var lessons = await GetStudentLessonsAsync(user.StudentId.Value, from, to);
            return Ok(lessons.Select(LessonsController.ToDto));
        }

        if (user.TeacherId.HasValue)
        {
            var lessons = await _lessonRepository.GetAllAsync(from, to, null, null, user.TeacherId.Value);
            return Ok(lessons.Select(LessonsController.ToDto));
        }

        return NotFound();
    }

    private async Task<List<Lesson>> GetStudentLessonsAsync(int studentId, DateOnly? from, DateOnly? to)
    {
        var activeEnrollments = await _enrollmentRepository.GetActiveByStudentIdAsync(studentId);

        var result = new List<Lesson>();
        foreach (var enrollment in activeEnrollments)
        {
            var enrolledDate = DateOnly.FromDateTime(enrollment.EnrolledDate);
            var effectiveFrom = from.HasValue && from.Value > enrolledDate ? from.Value : enrolledDate;

            var lessons = await _lessonRepository.GetAllAsync(effectiveFrom, to, enrollment.SubjectGroupId, null, null);
            result.AddRange(lessons);
        }

        return result.OrderBy(l => l.Date).ThenBy(l => l.StartTime).ToList();
    }
}
