using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public interface IMessagingPolicyRepository
{
    Task<IReadOnlyList<MessagingPolicyRule>> GetAllAsync();

    /// A pair with no stored row defaults to allowed.
    Task<bool> IsPairAllowedAsync(string roleA, string roleB);
    Task ReplaceAllAsync(IReadOnlyList<(string RoleA, string RoleB, bool Allowed)> rules, DateTime now);
}
