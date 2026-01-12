using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public RefreshTokenRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task AddRefreshToken(RefreshToken refreshToken)
        {
            await _db.Set<RefreshToken>().AddAsync(refreshToken);
            await _db.SaveChangesAsync();
        }
        public async Task<RefreshToken?> GetRefreshTokenById(Guid id)
        {
            return await _db.Set<RefreshToken>().FindAsync(id);
        }
        public async Task<RefreshToken?> GetRefreshTokenByToken(string token)
        {
            return await _db.Set<RefreshToken>().FirstOrDefaultAsync(rt => rt.Token == token);
        }
        public async Task<List<RefreshToken>> GetRefreshTokensByUserId(int userId)
        {
            return await _db.Set<RefreshToken>().Where(rt => rt.User_Id == userId).ToListAsync();
        }
        public async Task<RefreshToken> UpdateRefreshToken(RefreshToken refreshToken)
        {
            _db.Set<RefreshToken>().Update(refreshToken);
            await _db.SaveChangesAsync();
            return refreshToken;
        }
        public async Task<bool> DeleteRefreshToken(Guid id) {
            var refreshToken = await _db.Set<RefreshToken>().FindAsync(id);
            if (refreshToken == null) return false;

            _db.Set<RefreshToken>().Remove(refreshToken);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
