using Microsoft.EntityFrameworkCore;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Interfaces;

namespace ProductionCalculator.Data.Repositories
{
	public class VerificationCodeRepository : IVerificationCodeRepository
	{
		private readonly ProductionCalculatorDbContext _db;

		public VerificationCodeRepository(ProductionCalculatorDbContext db)
		{
			_db = db;
		}

		public async Task AddVerificationCode(VerificationCode verificationCode)
		{
			await _db.Set<VerificationCode>().AddAsync(verificationCode);
			await _db.SaveChangesAsync();
		}
        public async Task UpdateVerificationCode(VerificationCode verificationCode)
        {
            _db.Set<VerificationCode>().Update(verificationCode);
            await _db.SaveChangesAsync();
        }
		public async Task<VerificationCode?> GetVerificationCodeById(Guid id)
		{
			return await _db.Set<VerificationCode>().FirstOrDefaultAsync(vc => vc.Code_Id == id);
		}

		public async Task<VerificationCode?> GetVerificationCodeByCodeHash(string codeHash)
		{
			return await _db.Set<VerificationCode>().FirstOrDefaultAsync(vc => vc.Code_Hash == codeHash);
		}

		public async Task<List<VerificationCode>> GetVerificationCodesByUserId(int userId)
		{
			return await _db.Set<VerificationCode>().Where(vc => vc.User_Id == userId).ToListAsync();
		}

		public async Task<bool> DeleteVerificationCode(Guid id)
		{
			var entity = await _db.Set<VerificationCode>().FirstOrDefaultAsync(vc => vc.Code_Id == id);
			if (entity == null)
				return false;
			_db.Set<VerificationCode>().Remove(entity);
			await _db.SaveChangesAsync();
			return true;
		}
	}
}
