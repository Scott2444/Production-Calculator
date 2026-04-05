namespace ProductionCalculator.Business.APIModels
{
    public class UserResponse
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Puid { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public int ProjectCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
