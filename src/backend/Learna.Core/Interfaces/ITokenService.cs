using System.Security.Claims;

namespace Learna.Core.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(int userId, string email, IEnumerable<string> roles, int? studentId = null, int? guardianId = null, int? teacherId = null);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}
