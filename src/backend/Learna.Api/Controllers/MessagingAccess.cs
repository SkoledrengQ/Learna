using Learna.Core.Entities;
using Learna.Core.Interfaces;

namespace Learna.Api.Controllers;

internal static class MessagingAccess
{
    public static IReadOnlyList<string> RolesOf(User user) => user.UserRoles.Select(ur => ur.Role.Name).ToList();

    /// Multi-role users are allowed if ANY of their roles forms an allowed pair with ANY of the other user's roles.
    public static async Task<bool> IsPairAllowedAsync(IReadOnlyList<string> rolesA, IReadOnlyList<string> rolesB, IMessagingPolicyRepository policy)
    {
        foreach (var roleA in rolesA)
            foreach (var roleB in rolesB)
                if (await policy.IsPairAllowedAsync(roleA, roleB))
                    return true;
        return false;
    }

    /// Every pairwise combination of participants' role sets must be allowed (used at conversation
    /// creation and at group membership changes).
    public static async Task<bool> AllPairsAllowedAsync(IReadOnlyList<IReadOnlyList<string>> participantRoleSets, IMessagingPolicyRepository policy)
    {
        for (var i = 0; i < participantRoleSets.Count; i++)
            for (var j = i + 1; j < participantRoleSets.Count; j++)
                if (!await IsPairAllowedAsync(participantRoleSets[i], participantRoleSets[j], policy))
                    return false;
        return true;
    }
}
