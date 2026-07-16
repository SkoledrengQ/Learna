using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Learna.Core.Interfaces;
using Learna.Api.DTOs;

namespace Learna.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/school-settings")]
public class SchoolSettingsController : ControllerBase
{
    private static readonly Regex HexColorPattern = new(@"^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    private readonly ISchoolSettingsRepository _repository;

    public SchoolSettingsController(ISchoolSettingsRepository repository)
    {
        _repository = repository;
    }

    private static SchoolSettingsDto ToDto(Core.Entities.SchoolSettings settings) => new(
        settings.SchoolName,
        settings.PrimaryColor
    );

    [HttpGet]
    public async Task<ActionResult<SchoolSettingsDto>> Get()
    {
        var settings = await _repository.GetAsync();
        return Ok(ToDto(settings));
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SchoolSettingsDto>> Update(UpdateSchoolSettingsDto updateDto)
    {
        if (string.IsNullOrWhiteSpace(updateDto.SchoolName))
        {
            return BadRequest(new { message = "School name is required." });
        }

        if (!HexColorPattern.IsMatch(updateDto.PrimaryColor))
        {
            return BadRequest(new { message = "Primary color must be a hex value in #RRGGBB format." });
        }

        var updated = await _repository.UpdateAsync(updateDto.SchoolName, updateDto.PrimaryColor);
        return Ok(ToDto(updated));
    }
}
