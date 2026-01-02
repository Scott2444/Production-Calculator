namespace ProductionCalculator.Business.Interfaces
{
    public interface ICurrentUserService
    {
        int? UserId { get; }
        string? UserPuid { get; }
    }   
}