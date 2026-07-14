using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Api.DTOs;

namespace Learna.Api.Controllers;

[Authorize] // Require authentication for all endpoints
[ApiController]
[Route("api/school-years")]
public class SchoolYearsController : ControllerBase
{
    private readonly ISchoolYearRepository _schoolYearRepository;
    private readonly ITermRepository _termRepository;

    public SchoolYearsController(ISchoolYearRepository schoolYearRepository, ITermRepository termRepository)
    {
        _schoolYearRepository = schoolYearRepository;
        _termRepository = termRepository;
    }

    private static SchoolYearDto ToSchoolYearDto(SchoolYear schoolYear) => new(
        schoolYear.Id,
        schoolYear.Name,
        schoolYear.StartDate,
        schoolYear.EndDate,
        schoolYear.IsArchived
    );

    private static TermDto ToTermDto(Term term) => new(
        term.Id,
        term.SchoolYearId,
        term.Name,
        term.StartDate,
        term.EndDate
    );

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SchoolYearDto>>> GetAll()
    {
        var schoolYears = await _schoolYearRepository.GetAllAsync();
        return Ok(schoolYears.Select(ToSchoolYearDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SchoolYearDto>> GetById(int id)
    {
        var schoolYear = await _schoolYearRepository.GetByIdAsync(id);
        if (schoolYear == null)
        {
            return NotFound();
        }

        return Ok(ToSchoolYearDto(schoolYear));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SchoolYearDto>> Create(CreateSchoolYearDto createDto)
    {
        if (createDto.EndDate <= createDto.StartDate)
        {
            return BadRequest("End date must be after start date.");
        }

        var schoolYear = new SchoolYear
        {
            Name = createDto.Name,
            StartDate = createDto.StartDate,
            EndDate = createDto.EndDate
        };

        var createdSchoolYear = await _schoolYearRepository.CreateAsync(schoolYear);

        return CreatedAtAction(nameof(GetById), new { id = createdSchoolYear.Id }, ToSchoolYearDto(createdSchoolYear));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SchoolYearDto>> Update(int id, UpdateSchoolYearDto updateDto)
    {
        if (updateDto.EndDate <= updateDto.StartDate)
        {
            return BadRequest("End date must be after start date.");
        }

        var existingSchoolYear = await _schoolYearRepository.GetByIdAsync(id);
        if (existingSchoolYear == null)
        {
            return NotFound();
        }

        existingSchoolYear.Name = updateDto.Name;
        existingSchoolYear.StartDate = updateDto.StartDate;
        existingSchoolYear.EndDate = updateDto.EndDate;
        existingSchoolYear.IsArchived = updateDto.IsArchived;

        var updatedSchoolYear = await _schoolYearRepository.UpdateAsync(existingSchoolYear);

        return Ok(ToSchoolYearDto(updatedSchoolYear));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _schoolYearRepository.DeleteAsync(id);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet("{schoolYearId}/terms")]
    public async Task<ActionResult<IEnumerable<TermDto>>> GetTerms(int schoolYearId)
    {
        var schoolYear = await _schoolYearRepository.GetByIdAsync(schoolYearId);
        if (schoolYear == null)
        {
            return NotFound();
        }

        var terms = await _termRepository.GetBySchoolYearIdAsync(schoolYearId);
        return Ok(terms.Select(ToTermDto));
    }

    [HttpPost("{schoolYearId}/terms")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TermDto>> AddTerm(int schoolYearId, CreateTermDto createDto)
    {
        var schoolYear = await _schoolYearRepository.GetByIdAsync(schoolYearId);
        if (schoolYear == null)
        {
            return NotFound();
        }

        var validationError = ValidateTermWithinSchoolYear(createDto.StartDate, createDto.EndDate, schoolYear);
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        var term = new Term
        {
            SchoolYearId = schoolYearId,
            Name = createDto.Name,
            StartDate = createDto.StartDate,
            EndDate = createDto.EndDate
        };

        var createdTerm = await _termRepository.CreateAsync(term);

        return CreatedAtAction(nameof(GetTerms), new { schoolYearId }, ToTermDto(createdTerm));
    }

    [HttpPut("{schoolYearId}/terms/{termId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TermDto>> UpdateTerm(int schoolYearId, int termId, UpdateTermDto updateDto)
    {
        var schoolYear = await _schoolYearRepository.GetByIdAsync(schoolYearId);
        if (schoolYear == null)
        {
            return NotFound();
        }

        var existingTerm = await _termRepository.GetByIdAsync(termId);
        if (existingTerm == null || existingTerm.SchoolYearId != schoolYearId)
        {
            return NotFound();
        }

        var validationError = ValidateTermWithinSchoolYear(updateDto.StartDate, updateDto.EndDate, schoolYear);
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        existingTerm.Name = updateDto.Name;
        existingTerm.StartDate = updateDto.StartDate;
        existingTerm.EndDate = updateDto.EndDate;

        var updatedTerm = await _termRepository.UpdateAsync(existingTerm);

        return Ok(ToTermDto(updatedTerm));
    }

    [HttpDelete("{schoolYearId}/terms/{termId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveTerm(int schoolYearId, int termId)
    {
        var existingTerm = await _termRepository.GetByIdAsync(termId);
        if (existingTerm == null || existingTerm.SchoolYearId != schoolYearId)
        {
            return NotFound();
        }

        await _termRepository.DeleteAsync(termId);
        return NoContent();
    }

    private static string? ValidateTermWithinSchoolYear(DateTime startDate, DateTime endDate, SchoolYear schoolYear)
    {
        if (endDate <= startDate)
        {
            return "End date must be after start date.";
        }

        if (startDate < schoolYear.StartDate || endDate > schoolYear.EndDate)
        {
            return "Term dates must fall within the school year's start and end dates.";
        }

        return null;
    }
}
