using System;

namespace ProductionCalculator.Business.Models
{
    public class User
    {
        public required int User_Id { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Password_Hash { get; set; }
        public required DateTime Created_At { get; set; }
    }
}
