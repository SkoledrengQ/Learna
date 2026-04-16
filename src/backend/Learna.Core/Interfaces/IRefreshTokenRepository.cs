using Learna.Core.Entities;

namespace Learna.Core.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken);
    Task RevokeAsync(string token);
    Task RevokeAllUserTokensAsync(int userId);
    Task CleanupExpiredTokensAsync();
}
