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
    private readonly IGuardianRepository _guardianRepository;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(
        IStudentRepository repository,
        IGuardianRepository guardianRepository,
        ILogger<StudentsController> logger)
    {
        _repository = repository;
        _guardianRepository = guardianRepository;
        _logger = logger;
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

    private static StudentDto ToStudentDto(Student student) => new(
        student.Id,
        ToDto(student.Name),
        student.Email,
        student.StudentId,
        student.IdCardNumber,
        student.DateOfBirth,
        student.EnrollmentDate,
        student.PhoneNumber,
        student.Address,
        student.Height,
        student.Weight
    );

    private static StudentGuardianDto ToStudentGuardianDto(StudentGuardian link) => new(
        link.GuardianId,
        ToDto(link.Guardian.Name),
        link.Guardian.Email,
        link.Guardian.PhoneNumber,
        link.Relationship,
        link.IsPrimaryContact
    );

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StudentDto>>> GetAll()
    {
        var students = await _repository.GetAllAsync();
        return Ok(students.Select(ToStudentDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StudentDto>> GetById(int id)
    {
        var student = await _repository.GetByIdAsync(id);
        if (student == null)
        {
            return NotFound();
        }

        return Ok(ToStudentDto(student));
    }

    [HttpPost]
    public async Task<ActionResult<StudentDto>> Create(CreateStudentDto createDto)
    {
        // Generate unique student ID
        var studentId = await GenerateStudentIdAsync();

        var student = new Student
        {
            Name = ToEntity(createDto.Name),
            Email = createDto.Email,
            StudentId = studentId,
            IdCardNumber = createDto.IdCardNumber,
            DateOfBirth = createDto.DateOfBirth,
            EnrollmentDate = createDto.EnrollmentDate,
            PhoneNumber = createDto.PhoneNumber,
            Address = createDto.Address,
            Height = createDto.Height,
            Weight = createDto.Weight
        };

        var createdStudent = await _repository.CreateAsync(student);

        return CreatedAtAction(nameof(GetById), new { id = createdStudent.Id }, ToStudentDto(createdStudent));
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
        existingStudent.Name = ToEntity(updateDto.Name);
        existingStudent.Email = updateDto.Email;
        existingStudent.IdCardNumber = updateDto.IdCardNumber;
        existingStudent.DateOfBirth = updateDto.DateOfBirth;
        existingStudent.PhoneNumber = updateDto.PhoneNumber;
        existingStudent.Address = updateDto.Address;
        existingStudent.Height = updateDto.Height;
        existingStudent.Weight = updateDto.Weight;

        var updatedStudent = await _repository.UpdateAsync(existingStudent);

        return Ok(ToStudentDto(updatedStudent));
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

    [HttpGet("{studentId}/guardians")]
    public async Task<ActionResult<IEnumerable<StudentGuardianDto>>> GetGuardians(int studentId)
    {
        var student = await _repository.GetByIdWithGuardiansAsync(studentId);
        if (student == null)
        {
            return NotFound();
        }

        return Ok(student.StudentGuardians.Select(ToStudentGuardianDto));
    }

    [HttpPost("{studentId}/guardians")]
    public async Task<ActionResult<StudentGuardianDto>> AddGuardian(int studentId, CreateGuardianDto createDto)
    {
        var student = await _repository.GetByIdAsync(studentId);
        if (student == null)
        {
            return NotFound();
        }

        var guardian = new Guardian
        {
            Name = ToEntity(createDto.Name),
            Email = createDto.Email,
            PhoneNumber = createDto.PhoneNumber
        };

        var createdGuardian = await _guardianRepository.CreateAsync(guardian);

        var link = new StudentGuardian
        {
            StudentId = studentId,
            GuardianId = createdGuardian.Id,
            Relationship = createDto.Relationship,
            IsPrimaryContact = createDto.IsPrimaryContact
        };
        link = await _guardianRepository.LinkAsync(link);
        link.Guardian = createdGuardian;

        return CreatedAtAction(nameof(GetGuardians), new { studentId }, ToStudentGuardianDto(link));
    }

    [HttpPost("{studentId}/guardians/{guardianId}/link")]
    public async Task<ActionResult<StudentGuardianDto>> LinkExistingGuardian(int studentId, int guardianId, LinkGuardianDto linkDto)
    {
        var student = await _repository.GetByIdAsync(studentId);
        if (student == null)
        {
            return NotFound();
        }

        var guardian = await _guardianRepository.GetByIdAsync(guardianId);
        if (guardian == null)
        {
            return NotFound();
        }

        var existingLink = await _guardianRepository.GetLinkAsync(studentId, guardianId);
        if (existingLink != null)
        {
            return Conflict("Guardian is already linked to this student.");
        }

        var link = new StudentGuardian
        {
            StudentId = studentId,
            GuardianId = guardianId,
            Relationship = linkDto.Relationship,
            IsPrimaryContact = linkDto.IsPrimaryContact
        };
        link = await _guardianRepository.LinkAsync(link);
        link.Guardian = guardian;

        return CreatedAtAction(nameof(GetGuardians), new { studentId }, ToStudentGuardianDto(link));
    }

    [HttpPut("{studentId}/guardians/{guardianId}")]
    public async Task<ActionResult<StudentGuardianDto>> UpdateGuardian(int studentId, int guardianId, UpdateGuardianDto updateDto)
    {
        var link = await _guardianRepository.GetLinkAsync(studentId, guardianId);
        if (link == null)
        {
            return NotFound();
        }

        var guardian = link.Guardian;
        guardian.Name = ToEntity(updateDto.Name);
        guardian.Email = updateDto.Email;
        guardian.PhoneNumber = updateDto.PhoneNumber;
        await _guardianRepository.UpdateAsync(guardian);

        link.Relationship = updateDto.Relationship;
        link.IsPrimaryContact = updateDto.IsPrimaryContact;
        link = await _guardianRepository.UpdateLinkAsync(link);
        link.Guardian = guardian;

        return Ok(ToStudentGuardianDto(link));
    }

    [HttpDelete("{studentId}/guardians/{guardianId}")]
    public async Task<IActionResult> RemoveGuardian(int studentId, int guardianId)
    {
        var result = await _guardianRepository.UnlinkAsync(studentId, guardianId);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
