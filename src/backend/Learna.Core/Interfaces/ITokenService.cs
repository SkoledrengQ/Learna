using System.Security.Claims;

namespace Learna.Core.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(int userId, string email, IEnumerable<string> roles, int? studentId = null);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}
