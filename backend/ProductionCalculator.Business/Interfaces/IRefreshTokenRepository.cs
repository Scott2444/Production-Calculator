using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task AddRefreshToken(RefreshToken refreshToken);
        Task<RefreshToken?> GetRefreshTokenById(Guid id);
        Task<RefreshToken?> GetRefreshTokenByToken(string token);
        Task<List<RefreshToken>> GetRefreshTokensByUserId(int userId);
        Task<RefreshToken> UpdateRefreshToken(RefreshToken refreshToken);
        Task<bool> DeleteRefreshToken(Guid id);
    }
}