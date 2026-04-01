using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IPasswordResetTokenRepository
    {
        Task AddPasswordResetToken(PasswordResetToken passwordResetToken);
        Task UpdatePasswordResetToken(PasswordResetToken passwordResetToken);
        Task<PasswordResetToken?> GetPasswordResetTokenByUserId(int userId);
        Task<PasswordResetToken?> GetPasswordResetTokenByTokenHash(string tokenHash);
        Task<bool> DeletePasswordResetToken(Guid resetId);
    }
}