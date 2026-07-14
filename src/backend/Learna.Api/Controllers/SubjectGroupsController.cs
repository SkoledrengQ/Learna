using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Api.DTOs;

namespace Learna.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/subject-groups")]
public class SubjectGroupsController : ControllerBase
{
    private readonly ISubjectGroupRepository _subjectGroupRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ISchoolClassRepository _classRepository;
    private readonly IClassMembershipRepository _membershipRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ITermRepository _termRepository;
    private readonly ITeacherRepository _teacherRepository;

    public SubjectGroupsController(
        ISubjectGroupRepository subjectGroupRepository,
        IEnrollmentRepository enrollmentRepository,
        ISchoolClassRepository classRepository,
        IClassMembershipRepository membershipRepository,
        IStudentRepository studentRepository,
        ISubjectRepository subjectRepository,
        ITermRepository termRepository,
        ITeacherRepository teacherRepository)
    {
        _subjectGroupRepository = subjectGroupRepository;
        _enrollmentRepository = enrollmentRepository;
        _classRepository = classRepository;
        _membershipRepository = membershipRepository;
        _studentRepository = studentRepository;
        _subjectRepository = subjectRepository;
        _termRepository = termRepository;
        _teacherRepository = teacherRepository;
    }

    private static SubjectGroupDto ToDto(SubjectGroup group) => new(
        group.Id,
        group.SubjectId,
        group.TermId,
        group.Name,
        group.TeacherId
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

    private static EnrollmentDto ToEnrollmentDto(Enrollment enrollment) => new(
        enrollment.Id,
        ToStudentSummaryDto(enrollment.Student),
        enrollment.EnrolledDate,
        enrollment.UnenrolledDate
    );

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SubjectGroupDto>>> GetAll([FromQuery] int? termId, [FromQuery] int? subjectId)
    {
        var groups = await _subjectGroupRepository.GetAllAsync(termId, subjectId);
        return Ok(groups.Select(ToDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SubjectGroupDto>> GetById(int id)
    {
        var group = await _subjectGroupRepository.GetByIdAsync(id);
        if (group == null)
        {
            return NotFound();
        }

        return Ok(ToDto(group));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SubjectGroupDto>> Create(CreateSubjectGroupDto createDto)
    {
        if (await _subjectRepository.GetByIdAsync(createDto.SubjectId) == null)
        {
            return BadRequest("Subject not found.");
        }

        if (await _termRepository.GetByIdAsync(createDto.TermId) == null)
        {
            return BadRequest("Term not found.");
        }

        if (createDto.TeacherId.HasValue &&
            await _teacherRepository.GetByIdAsync(createDto.TeacherId.Value) == null)
        {
            return BadRequest("Teacher not found.");
        }

        var group = new SubjectGroup
        {
            SubjectId = createDto.SubjectId,
            TermId = createDto.TermId,
            Name = createDto.Name,
            TeacherId = createDto.TeacherId
        };

        var created = await _subjectGroupRepository.CreateAsync(group);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToDto(created));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SubjectGroupDto>> Update(int id, UpdateSubjectGroupDto updateDto)
    {
        var existing = await _subjectGroupRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        if (updateDto.TeacherId.HasValue &&
            await _teacherRepository.GetByIdAsync(updateDto.TeacherId.Value) == null)
        {
            return BadRequest("Teacher not found.");
        }

        existing.Name = updateDto.Name;
        existing.TeacherId = updateDto.TeacherId;

        var updated = await _subjectGroupRepository.UpdateAsync(existing);

        return Ok(ToDto(updated));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _subjectGroupRepository.DeleteAsync(id);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet("{id}/enrollments")]
    public async Task<ActionResult<IEnumerable<EnrollmentDto>>> GetEnrollments(int id, [FromQuery] bool includeHistorical = false)
    {
        var group = await _subjectGroupRepository.GetByIdAsync(id);
        if (group == null)
        {
            return NotFound();
        }

        var enrollments = await _enrollmentRepository.GetBySubjectGroupIdAsync(id, includeHistorical);
        return Ok(enrollments.Select(ToEnrollmentDto));
    }

    [HttpPost("{id}/enrollments")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EnrollmentDto>> AddEnrollment(int id, EnrollStudentDto dto)
    {
        var group = await _subjectGroupRepository.GetByIdAsync(id);
        if (group == null)
        {
            return NotFound();
        }

        var student = await _studentRepository.GetByIdAsync(dto.StudentId);
        if (student == null)
        {
            return BadRequest("Student not found.");
        }

        var existing = await _enrollmentRepository.GetActiveBySubjectGroupAndStudentAsync(id, dto.StudentId);
        if (existing != null)
        {
            return Conflict("Student is already actively enrolled in this subject group.");
        }

        var enrollment = new Enrollment
        {
            StudentId = dto.StudentId,
            SubjectGroupId = id,
            EnrolledDate = dto.EnrolledDate ?? DateTime.UtcNow,
            UnenrolledDate = null
        };

        var created = await _enrollmentRepository.AddAsync(enrollment);
        created.Student = student;

        return CreatedAtAction(nameof(GetEnrollments), new { id }, ToEnrollmentDto(created));
    }

    [HttpDelete("{id}/enrollments/{studentId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveEnrollment(int id, int studentId)
    {
        var existing = await _enrollmentRepository.GetActiveBySubjectGroupAndStudentAsync(id, studentId);
        if (existing == null)
        {
            return NotFound();
        }

        await _enrollmentRepository.CloseAsync(existing, DateTime.UtcNow);
        return NoContent();
    }

    [HttpPost("{id}/enroll-class/{classId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<EnrollClassResultDto>> EnrollClass(int id, int classId)
    {
        var group = await _subjectGroupRepository.GetByIdAsync(id);
        if (group == null)
        {
            return NotFound();
        }

        var schoolClass = await _classRepository.GetByIdAsync(classId);
        if (schoolClass == null)
        {
            return BadRequest("Class not found.");
        }

        var activeMembers = await _membershipRepository.GetByClassIdAsync(classId, includeHistorical: false);

        var enrolledCount = 0;
        var skippedCount = 0;

        foreach (var member in activeMembers)
        {
            var existing = await _enrollmentRepository.GetActiveBySubjectGroupAndStudentAsync(id, member.StudentId);
            if (existing != null)
            {
                skippedCount++;
                continue;
            }

            await _enrollmentRepository.AddAsync(new Enrollment
            {
                StudentId = member.StudentId,
                SubjectGroupId = id,
                EnrolledDate = DateTime.UtcNow,
                UnenrolledDate = null
            });
            enrolledCount++;
        }

        return Ok(new EnrollClassResultDto(enrolledCount, skippedCount));
    }
}
