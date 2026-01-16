using ProductionCalculator.Business.Models;

namespace ProductionCalculator.Business.Interfaces
{
    public interface IVerificationCodeRepository
    {
        Task AddVerificationCode(VerificationCode verificationCode);
        Task UpdateVerificationCode(VerificationCode verificationCode);
        Task<VerificationCode?> GetVerificationCodeById(Guid id);
        Task<VerificationCode?> GetVerificationCodeByCodeHash(string codeHash);
        Task<List<VerificationCode>> GetVerificationCodesByUserId(int userId);
        Task<bool> DeleteVerificationCode(Guid id);
    }
}