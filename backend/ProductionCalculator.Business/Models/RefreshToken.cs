namespace ProductionCalculator.Business.Models
{
    public class RefreshToken
    {
        public required Guid Token_Id { get; set; }
        public required int User_Id { get; set; }
        public required string Token { get; set; }
        public required DateTime Expires_At { get; set; }
        public required DateTime Created_At { get; set; }
        public DateTime? Revoked_At { get; set; }
    }
}
