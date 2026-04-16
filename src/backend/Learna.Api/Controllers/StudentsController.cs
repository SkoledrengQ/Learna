using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Api.DTOs;

namespace Learna.Api.Controllers;

[Authorize] // Require authentication for all endpoints
[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly IStudentRepository _repository;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(IStudentRepository repository, ILogger<StudentsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StudentDto>>> GetAll()
    {
        var students = await _repository.GetAllAsync();
        var studentDtos = students.Select(s => new StudentDto(
            s.Id,
            s.FirstName,
            s.LastName,
            s.Email,
            s.StudentId,
            s.IdCardNumber,
            s.DateOfBirth,
            s.ParentPhoneNumber,
            s.EnrollmentDate,
            s.GradeLevel
        ));
        return Ok(studentDtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StudentDto>> GetById(int id)
    {
        var student = await _repository.GetByIdAsync(id);
        if (student == null)
        {
            return NotFound();
        }

        var studentDto = new StudentDto(
            student.Id,
            student.FirstName,
            student.LastName,
            student.Email,
            student.StudentId,
            student.IdCardNumber,
            student.DateOfBirth,
            student.ParentPhoneNumber,
            student.EnrollmentDate,
            student.GradeLevel
        );
        return Ok(studentDto);
    }

    [HttpPost]
    public async Task<ActionResult<StudentDto>> Create(CreateStudentDto createDto)
    {
        // Generate unique student ID
        var studentId = await GenerateStudentIdAsync();

        var student = new Student
        {
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            Email = createDto.Email,
            StudentId = studentId,
            IdCardNumber = createDto.IdCardNumber,
            DateOfBirth = createDto.DateOfBirth,
            ParentPhoneNumber = createDto.ParentPhoneNumber,
            EnrollmentDate = createDto.EnrollmentDate,
            GradeLevel = createDto.GradeLevel
        };

        var createdStudent = await _repository.CreateAsync(student);

        var studentDto = new StudentDto(
            createdStudent.Id,
            createdStudent.FirstName,
            createdStudent.LastName,
            createdStudent.Email,
            createdStudent.StudentId,
            createdStudent.IdCardNumber,
            createdStudent.DateOfBirth,
            createdStudent.ParentPhoneNumber,
            createdStudent.EnrollmentDate,
            createdStudent.GradeLevel
        );

        return CreatedAtAction(nameof(GetById), new { id = studentDto.Id }, studentDto);
    }

    private async Task<string> GenerateStudentIdAsync()
    {
        var year = DateTime.Now.Year;
        var students = await _repository.GetAllAsync();
        var count = students.Count() + 1;

        string studentId;
        do
        {
            studentId = $"{year}-{count:D4}";
            count++;
        } while (await _repository.StudentIdExistsAsync(studentId));

        return studentId;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<StudentDto>> Update(int id, UpdateStudentDto updateDto)
    {
        var existingStudent = await _repository.GetByIdAsync(id);
        if (existingStudent == null)
        {
            return NotFound();
        }

        // Update only the fields that are allowed to be updated
        existingStudent.FirstName = updateDto.FirstName;
        existingStudent.LastName = updateDto.LastName;
        existingStudent.Email = updateDto.Email;
        existingStudent.IdCardNumber = updateDto.IdCardNumber;
        existingStudent.DateOfBirth = updateDto.DateOfBirth;
        existingStudent.ParentPhoneNumber = updateDto.ParentPhoneNumber;
        existingStudent.GradeLevel = updateDto.GradeLevel;

        var updatedStudent = await _repository.UpdateAsync(existingStudent);

        var studentDto = new StudentDto(
            updatedStudent.Id,
            updatedStudent.FirstName,
            updatedStudent.LastName,
            updatedStudent.Email,
            updatedStudent.StudentId,
            updatedStudent.IdCardNumber,
            updatedStudent.DateOfBirth,
            updatedStudent.ParentPhoneNumber,
            updatedStudent.EnrollmentDate,
            updatedStudent.GradeLevel
        );

        return Ok(studentDto);
    }

    [HttpDelete("{id}")]
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
