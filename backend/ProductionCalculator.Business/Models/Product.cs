using System;

namespace ProductionCalculator.Business.Models
{
    public class Product
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required int Product_Type_Id { get; set; }
        public UserAttributes? User_Attributes { get; set; }
        public int? Project_Id { get; set; }
    }
}
