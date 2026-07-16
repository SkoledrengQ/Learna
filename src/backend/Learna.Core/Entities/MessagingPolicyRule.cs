namespace Learna.Core.Entities;

/// School-configurable role-pair permission for messaging (see dev-scope.md Messaging section).
/// RoleA/RoleB are stored in alphabetical order (a canonical, unordered pair); same-role pairs
/// (e.g. "Student"/"Student") are valid rows. A pair with no row defaults to allowed.
public class MessagingPolicyRule
{
    public int Id { get; set; }
    public required string RoleA { get; set; }
    public required string RoleB { get; set; }
    public bool Allowed { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
