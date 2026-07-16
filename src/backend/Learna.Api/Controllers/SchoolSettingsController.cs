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

    /// The fixed role set the messaging policy matrix is drawn over; matches the seeded Role table.
    private static readonly string[] MessagingRoles = { "Admin", "Teacher", "Student", "Parent" };

    private readonly ISchoolSettingsRepository _repository;
    private readonly IMessagingPolicyRepository _messagingPolicy;
    private readonly TimeProvider _timeProvider;

    public SchoolSettingsController(ISchoolSettingsRepository repository, IMessagingPolicyRepository messagingPolicy, TimeProvider timeProvider)
    {
        _repository = repository;
        _messagingPolicy = messagingPolicy;
        _timeProvider = timeProvider;
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

    [HttpGet("messaging-policy")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MessagingPolicyDto>> GetMessagingPolicy()
    {
        var rules = await _messagingPolicy.GetAllAsync();
        return Ok(new MessagingPolicyDto(BuildFullMatrix(rules)));
    }

    [HttpPut("messaging-policy")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MessagingPolicyDto>> UpdateMessagingPolicy(UpdateMessagingPolicyDto updateDto)
    {
        foreach (var rule in updateDto.Rules)
        {
            if (!MessagingRoles.Contains(rule.RoleA) || !MessagingRoles.Contains(rule.RoleB))
                return BadRequest(new { message = "Unknown role in messaging policy rule." });
        }

        await _messagingPolicy.ReplaceAllAsync(updateDto.Rules.Select(r => (r.RoleA, r.RoleB, r.Allowed)).ToList(), _timeProvider.GetUtcNow().UtcDateTime);
        var rules = await _messagingPolicy.GetAllAsync();
        return Ok(new MessagingPolicyDto(BuildFullMatrix(rules)));
    }

    /// Fills in every canonical (unordered, including same-role) pair among the known roles,
    /// defaulting to Allowed = true for any pair with no stored row.
    private static IReadOnlyList<MessagingPolicyRuleDto> BuildFullMatrix(IReadOnlyList<Core.Entities.MessagingPolicyRule> rules)
    {
        var result = new List<MessagingPolicyRuleDto>();
        for (var i = 0; i < MessagingRoles.Length; i++)
        {
            for (var j = i; j < MessagingRoles.Length; j++)
            {
                var (a, b) = string.CompareOrdinal(MessagingRoles[i], MessagingRoles[j]) <= 0 ? (MessagingRoles[i], MessagingRoles[j]) : (MessagingRoles[j], MessagingRoles[i]);
                var stored = rules.FirstOrDefault(r => r.RoleA == a && r.RoleB == b);
                result.Add(new MessagingPolicyRuleDto(MessagingRoles[i], MessagingRoles[j], stored?.Allowed ?? true));
            }
        }
        return result;
    }
}
