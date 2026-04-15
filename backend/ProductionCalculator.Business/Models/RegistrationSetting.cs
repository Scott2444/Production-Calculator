namespace ProductionCalculator.Business.Models
{
    public class RegistrationSetting
    {
        public required int Settings_Id { get; set; }
        public bool Is_Registration_Enabled { get; set; }
        public required DateTime Last_Updated { get; set; }
    }
}