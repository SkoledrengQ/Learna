using Learna.Core.Entities;
using Learna.Core.Interfaces;
using Learna.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Learna.Infrastructure.Repositories;

public sealed class MessagingPolicyRepository(ApplicationDbContext context) : IMessagingPolicyRepository
{
    public async Task<IReadOnlyList<MessagingPolicyRule>> GetAllAsync() => await context.MessagingPolicyRules.OrderBy(r => r.RoleA).ThenBy(r => r.RoleB).ToListAsync();

    public async Task<bool> IsPairAllowedAsync(string roleA, string roleB)
    {
        var (a, b) = Normalize(roleA, roleB);
        var rule = await context.MessagingPolicyRules.FirstOrDefaultAsync(r => r.RoleA == a && r.RoleB == b);
        return rule?.Allowed ?? true;
    }

    public async Task ReplaceAllAsync(IReadOnlyList<(string RoleA, string RoleB, bool Allowed)> rules, DateTime now)
    {
        var existing = await context.MessagingPolicyRules.ToListAsync();
        foreach (var (roleA, roleB, allowed) in rules)
        {
            var (a, b) = Normalize(roleA, roleB);
            var rule = existing.FirstOrDefault(r => r.RoleA == a && r.RoleB == b);
            if (rule == null)
                context.MessagingPolicyRules.Add(new MessagingPolicyRule { RoleA = a, RoleB = b, Allowed = allowed, UpdatedAt = now });
            else
                rule.Allowed = allowed;
        }
        await context.SaveChangesAsync();
    }

    private static (string, string) Normalize(string roleA, string roleB) => string.CompareOrdinal(roleA, roleB) <= 0 ? (roleA, roleB) : (roleB, roleA);
}
