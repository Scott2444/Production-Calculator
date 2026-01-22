namespace ProductionCalculator.Business.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserPuid { get; }
        bool IsAuthenticated { get; }
        bool IsAdmin { get; }
    }   
}