using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Api.DTOs;

namespace Learna.Api.Controllers;

[Authorize] // Require authentication for all endpoints
[ApiController]
[Route("api/[controller]")]
public class TeachersController : ControllerBase
{
    private readonly ITeacherRepository _repository;

    public TeachersController(ITeacherRepository repository)
    {
        _repository = repository;
    }

    private static PersonNameDto ToDto(PersonName name) => new(
        name.Title,
        name.FirstName,
        name.LastName,
        name.FirstNameEnglish,
        name.LastNameEnglish,
        name.Nickname
    );

    private static PersonName ToEntity(PersonNameDto dto) => new()
    {
        Title = dto.Title,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        FirstNameEnglish = dto.FirstNameEnglish,
        LastNameEnglish = dto.LastNameEnglish,
        Nickname = dto.Nickname
    };

    private static TeacherDto ToTeacherDto(Teacher teacher) => new(
        teacher.Id,
        ToDto(teacher.Name),
        teacher.Email,
        teacher.PhoneNumber,
        teacher.EmployeeId
    );

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TeacherDto>>> GetAll()
    {
        var teachers = await _repository.GetAllAsync();
        return Ok(teachers.Select(ToTeacherDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TeacherDto>> GetById(int id)
    {
        var teacher = await _repository.GetByIdAsync(id);
        if (teacher == null)
        {
            return NotFound();
        }

        return Ok(ToTeacherDto(teacher));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TeacherDto>> Create(CreateTeacherDto createDto)
    {
        if (await _repository.EmailExistsAsync(createDto.Email))
        {
            return Conflict("A teacher with this email already exists.");
        }

        var teacher = new Teacher
        {
            Name = ToEntity(createDto.Name),
            Email = createDto.Email,
            PhoneNumber = createDto.PhoneNumber,
            EmployeeId = createDto.EmployeeId
        };

        var createdTeacher = await _repository.CreateAsync(teacher);

        return CreatedAtAction(nameof(GetById), new { id = createdTeacher.Id }, ToTeacherDto(createdTeacher));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TeacherDto>> Update(int id, UpdateTeacherDto updateDto)
    {
        var existingTeacher = await _repository.GetByIdAsync(id);
        if (existingTeacher == null)
        {
            return NotFound();
        }

        existingTeacher.Name = ToEntity(updateDto.Name);
        existingTeacher.Email = updateDto.Email;
        existingTeacher.PhoneNumber = updateDto.PhoneNumber;
        existingTeacher.EmployeeId = updateDto.EmployeeId;

        var updatedTeacher = await _repository.UpdateAsync(existingTeacher);

        return Ok(ToTeacherDto(updatedTeacher));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _repository.DeleteAsync(id);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
