using Microsoft.AspNetCore.Authorization;

namespace ProductionCalculator.API.Authorization
{
    public class OwnerOrAdminRequirement : IAuthorizationRequirement { }
}
