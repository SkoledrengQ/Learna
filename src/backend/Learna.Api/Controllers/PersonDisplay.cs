using Learna.Core.Entities;

namespace Learna.Api.Controllers;

/// Shared display-name formatting for messaging (directory, sender names, admin oversight).
internal static class PersonDisplay
{
    public static string Formal(PersonName name) => string.Join(" ", new[] { name.Title, name.FirstName, name.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

    public static string WithNickname(PersonName name) => string.IsNullOrWhiteSpace(name.Nickname) ? Formal(name) : $"{Formal(name)} ({name.Nickname})";

    /// Display name for a User in messaging contexts (directory, sender, admin oversight) — never the email (no PII beyond name/role).
    public static string ForUser(User user) => user.Teacher != null
        ? WithNickname(user.Teacher.Name)
        : user.Student != null
            ? WithNickname(user.Student.Name)
            : user.Guardian != null
                ? WithNickname(user.Guardian.Name)
                : (user.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault() ?? "User");
}
