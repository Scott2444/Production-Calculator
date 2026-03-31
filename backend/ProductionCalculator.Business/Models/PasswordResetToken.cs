namespace ProductionCalculator.Business.Models
{
    public class PasswordResetToken
    {
        public required Guid Reset_Id { get; set; }
        public required int User_Id { get; set; }
        public required string Token_Hash { get; set; }
        public required DateTime Created_At { get; set; }
        public required DateTime Expires_At { get; set; }
    }
}