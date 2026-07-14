using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Api.DTOs;

namespace Learna.Api.Controllers;

[Authorize] // Require authentication for all endpoints
[ApiController]
[Route("api/[controller]")]
public class SubjectsController : ControllerBase
{
    private readonly ISubjectRepository _repository;

    public SubjectsController(ISubjectRepository repository)
    {
        _repository = repository;
    }

    private static SubjectDto ToSubjectDto(Subject subject) => new(
        subject.Id,
        subject.Code,
        subject.NameEnglish,
        subject.NameThai,
        subject.Description
    );

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SubjectDto>>> GetAll()
    {
        var subjects = await _repository.GetAllAsync();
        return Ok(subjects.Select(ToSubjectDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SubjectDto>> GetById(int id)
    {
        var subject = await _repository.GetByIdAsync(id);
        if (subject == null)
        {
            return NotFound();
        }

        return Ok(ToSubjectDto(subject));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SubjectDto>> Create(CreateSubjectDto createDto)
    {
        if (await _repository.CodeExistsAsync(createDto.Code))
        {
            return Conflict("A subject with this code already exists.");
        }

        var subject = new Subject
        {
            Code = createDto.Code,
            NameEnglish = createDto.NameEnglish,
            NameThai = createDto.NameThai,
            Description = createDto.Description
        };

        var createdSubject = await _repository.CreateAsync(subject);

        return CreatedAtAction(nameof(GetById), new { id = createdSubject.Id }, ToSubjectDto(createdSubject));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SubjectDto>> Update(int id, UpdateSubjectDto updateDto)
    {
        var existingSubject = await _repository.GetByIdAsync(id);
        if (existingSubject == null)
        {
            return NotFound();
        }

        if (existingSubject.Code != updateDto.Code && await _repository.CodeExistsAsync(updateDto.Code))
        {
            return Conflict("A subject with this code already exists.");
        }

        existingSubject.Code = updateDto.Code;
        existingSubject.NameEnglish = updateDto.NameEnglish;
        existingSubject.NameThai = updateDto.NameThai;
        existingSubject.Description = updateDto.Description;

        var updatedSubject = await _repository.UpdateAsync(existingSubject);

        return Ok(ToSubjectDto(updatedSubject));
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
