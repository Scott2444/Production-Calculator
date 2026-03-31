using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Data.Repositories
{
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly ProductionCalculatorDbContext _db;

        public PasswordResetTokenRepository(ProductionCalculatorDbContext db)
        {
            _db = db;
        }

        public async Task AddPasswordResetToken(PasswordResetToken passwordResetToken)
        {
            await _db.Set<PasswordResetToken>().AddAsync(passwordResetToken);
            await _db.SaveChangesAsync();
        }

        public async Task UpdatePasswordResetToken(PasswordResetToken passwordResetToken)
        {
            _db.Set<PasswordResetToken>().Update(passwordResetToken);
            await _db.SaveChangesAsync();
        }

        public async Task<PasswordResetToken?> GetPasswordResetTokenByUserId(int userId)
        {
            return await _db.Set<PasswordResetToken>().FirstOrDefaultAsync(prt => prt.User_Id == userId);
        }

        public async Task<PasswordResetToken?> GetPasswordResetTokenByTokenHash(string tokenHash)
        {
            return await _db.Set<PasswordResetToken>().FirstOrDefaultAsync(prt => prt.Token_Hash == tokenHash);
        }

        public async Task<bool> DeletePasswordResetToken(Guid resetId)
        {
            var entity = await _db.Set<PasswordResetToken>().FirstOrDefaultAsync(prt => prt.Reset_Id == resetId);
            if (entity == null)
            {
                return false;
            }
            _db.Set<PasswordResetToken>().Remove(entity);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}