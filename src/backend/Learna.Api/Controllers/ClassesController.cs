using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Api.DTOs;

namespace Learna.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/classes")]
public class ClassesController : ControllerBase
{
    private readonly ISchoolClassRepository _classRepository;
    private readonly IClassMembershipRepository _membershipRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ISchoolYearRepository _schoolYearRepository;
    private readonly ITeacherRepository _teacherRepository;

    public ClassesController(
        ISchoolClassRepository classRepository,
        IClassMembershipRepository membershipRepository,
        IStudentRepository studentRepository,
        ISchoolYearRepository schoolYearRepository,
        ITeacherRepository teacherRepository)
    {
        _classRepository = classRepository;
        _membershipRepository = membershipRepository;
        _studentRepository = studentRepository;
        _schoolYearRepository = schoolYearRepository;
        _teacherRepository = teacherRepository;
    }

    private static SchoolClassDto ToDto(SchoolClass schoolClass) => new(
        schoolClass.Id,
        schoolClass.SchoolYearId,
        schoolClass.Name,
        schoolClass.Description,
        schoolClass.HomeroomTeacherId
    );

    private static StudentSummaryDto ToStudentSummaryDto(Student student) => new(
        student.Id,
        new PersonNameDto(
            student.Name.Title,
            student.Name.FirstName,
            student.Name.LastName,
            student.Name.FirstNameEnglish,
            student.Name.LastNameEnglish,
            student.Name.Nickname
        ),
        student.StudentId,
        student.Email
    );

    private static ClassMembershipDto ToMembershipDto(ClassMembership membership) => new(
        membership.Id,
        ToStudentSummaryDto(membership.Student),
        membership.JoinedDate,
        membership.LeftDate
    );

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SchoolClassDto>>> GetAll([FromQuery] int? schoolYearId)
    {
        var classes = await _classRepository.GetAllAsync(schoolYearId);
        return Ok(classes.Select(ToDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SchoolClassDto>> GetById(int id)
    {
        var schoolClass = await _classRepository.GetByIdAsync(id);
        if (schoolClass == null)
        {
            return NotFound();
        }

        return Ok(ToDto(schoolClass));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SchoolClassDto>> Create(CreateSchoolClassDto createDto)
    {
        var schoolYear = await _schoolYearRepository.GetByIdAsync(createDto.SchoolYearId);
        if (schoolYear == null)
        {
            return BadRequest("School year not found.");
        }

        if (createDto.HomeroomTeacherId.HasValue &&
            await _teacherRepository.GetByIdAsync(createDto.HomeroomTeacherId.Value) == null)
        {
            return BadRequest("Homeroom teacher not found.");
        }

        var schoolClass = new SchoolClass
        {
            SchoolYearId = createDto.SchoolYearId,
            Name = createDto.Name,
            Description = createDto.Description,
            HomeroomTeacherId = createDto.HomeroomTeacherId
        };

        var created = await _classRepository.CreateAsync(schoolClass);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToDto(created));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SchoolClassDto>> Update(int id, UpdateSchoolClassDto updateDto)
    {
        var existing = await _classRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        if (updateDto.HomeroomTeacherId.HasValue &&
            await _teacherRepository.GetByIdAsync(updateDto.HomeroomTeacherId.Value) == null)
        {
            return BadRequest("Homeroom teacher not found.");
        }

        existing.Name = updateDto.Name;
        existing.Description = updateDto.Description;
        existing.HomeroomTeacherId = updateDto.HomeroomTeacherId;

        var updated = await _classRepository.UpdateAsync(existing);

        return Ok(ToDto(updated));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _classRepository.DeleteAsync(id);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet("{id}/members")]
    public async Task<ActionResult<IEnumerable<ClassMembershipDto>>> GetMembers(int id, [FromQuery] bool includeHistorical = false)
    {
        var schoolClass = await _classRepository.GetByIdAsync(id);
        if (schoolClass == null)
        {
            return NotFound();
        }

        var members = await _membershipRepository.GetByClassIdAsync(id, includeHistorical);
        return Ok(members.Select(ToMembershipDto));
    }

    [HttpPost("{id}/members")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ClassMembershipDto>> AddMember(int id, AddClassMemberDto dto)
    {
        var schoolClass = await _classRepository.GetByIdAsync(id);
        if (schoolClass == null)
        {
            return NotFound();
        }

        var student = await _studentRepository.GetByIdAsync(dto.StudentId);
        if (student == null)
        {
            return BadRequest("Student not found.");
        }

        var existingInClass = await _membershipRepository.GetActiveByStudentAndClassAsync(dto.StudentId, id);
        if (existingInClass != null)
        {
            return Conflict("Student is already an active member of this class.");
        }

        var existingInYear = await _membershipRepository.GetActiveByStudentAndSchoolYearAsync(dto.StudentId, schoolClass.SchoolYearId);
        if (existingInYear != null)
        {
            return Conflict("Student already has an active class membership for this school year. Use move instead.");
        }

        var membership = new ClassMembership
        {
            StudentId = dto.StudentId,
            ClassId = id,
            JoinedDate = dto.JoinedDate ?? DateTime.UtcNow,
            LeftDate = null
        };

        var created = await _membershipRepository.AddAsync(membership);
        created.Student = student;

        return CreatedAtAction(nameof(GetMembers), new { id }, ToMembershipDto(created));
    }

    [HttpPost("{id}/members/{studentId}/move")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ClassMembershipDto>> MoveMember(int id, int studentId, MoveClassMemberDto dto)
    {
        var fromClass = await _classRepository.GetByIdAsync(id);
        if (fromClass == null)
        {
            return NotFound("Source class not found.");
        }

        var toClass = await _classRepository.GetByIdAsync(dto.ToClassId);
        if (toClass == null)
        {
            return BadRequest("Target class not found.");
        }

        var activeMembership = await _membershipRepository.GetActiveByStudentAndClassAsync(studentId, id);
        if (activeMembership == null)
        {
            return BadRequest("Student is not an active member of the source class.");
        }

        var activeInTargetYear = await _membershipRepository.GetActiveByStudentAndSchoolYearAsync(studentId, toClass.SchoolYearId);
        if (activeInTargetYear != null && activeInTargetYear.ClassId != id)
        {
            return Conflict("Student already has an active class membership in the target school year.");
        }

        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null)
        {
            return BadRequest("Student not found.");
        }

        var moveDate = dto.MoveDate ?? DateTime.UtcNow;
        var newMembership = await _membershipRepository.MoveAsync(studentId, id, dto.ToClassId, moveDate);
        newMembership.Student = student;

        return Ok(ToMembershipDto(newMembership));
    }

    [HttpDelete("{id}/members/{studentId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveMember(int id, int studentId)
    {
        var activeMembership = await _membershipRepository.GetActiveByStudentAndClassAsync(studentId, id);
        if (activeMembership == null)
        {
            return NotFound();
        }

        await _membershipRepository.CloseAsync(activeMembership, DateTime.UtcNow);
        return NoContent();
    }
}
