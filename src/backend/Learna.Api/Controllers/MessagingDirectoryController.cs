using System.Security.Claims;
using Learna.Api.DTOs;
using Learna.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Learna.Api.Controllers;

/// Messageable-people picker for starting conversations. Deliberately a separate route from
/// UsersController (which is Admin-only) since every role must be able to search this directory.
[Authorize]
[ApiController]
[Route("api/users/directory")]
public sealed class MessagingDirectoryController(IUserRepository users, IMessagingPolicyRepository policy) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DirectoryPersonDto>>> Search([FromQuery] string? search)
    {
        var callerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var caller = await users.GetByIdAsync(callerId);
        if (caller == null) return Forbid();
        var callerRoles = MessagingAccess.RolesOf(caller);

        var candidates = await users.GetAllAsync(search);
        var result = new List<DirectoryPersonDto>();
        foreach (var candidate in candidates)
        {
            if (candidate.Id == callerId || !candidate.IsActive) continue;
            var candidateRoles = MessagingAccess.RolesOf(candidate);
            if (candidateRoles.Count == 0) continue;
            if (!await MessagingAccess.IsPairAllowedAsync(callerRoles, candidateRoles, policy)) continue;
            result.Add(new DirectoryPersonDto(candidate.Id, PersonDisplay.ForUser(candidate), candidateRoles));
        }
        return Ok(result.OrderBy(p => p.Name).ToList());
    }
}
