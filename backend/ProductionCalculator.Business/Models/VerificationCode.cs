namespace ProductionCalculator.Business.Models
{
    public class VerificationCode
    {
        public required Guid Code_Id { get; set; }
        public required int User_Id { get; set; }
        public required string Code_Hash { get; set; }
        public required int Attempts { get; set; }
        public required DateTime Created_At { get; set; }
        public required DateTime Expires_At { get; set; }
    }
}
