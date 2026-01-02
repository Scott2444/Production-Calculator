using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Helpers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ProductionCalculator.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _repo;
        private readonly IRoleRepository _roleRepo;
        private readonly JwtHelper _jwtHelper;
        private readonly IProjectRepository _projectRepository;
        public AuthService
        (
            IUserRepository repo, 
            IRoleRepository roleRepo, 
            JwtHelper jwtHelper, 
            IProjectRepository projectRepository)
        {
            _repo = repo;
            _roleRepo = roleRepo;
            _jwtHelper = jwtHelper;
            _projectRepository = projectRepository;
        }
        /// <summary>
        /// Checks if the user is the owner of the resource identified by puid in the route.
        /// </summary>
        public async Task<bool> IsOwner(ClaimsPrincipal user, string? routePuid, string? route)
        {
            // Must be authenticated
            if (!user.Identity?.IsAuthenticated ?? true)
                return false;

            // Get the claim from the JWT
            var claimUserPuid = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var claimUserIdStr = user.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(claimUserIdStr) || string.IsNullOrEmpty(claimUserPuid))
                return false;
            int claimUserId = int.Parse(claimUserIdStr);

            // If accessing user resource, compare directly
            if (route?.Contains("/users/") == true)
            {
                return claimUserPuid != null && routePuid == claimUserPuid;
            }

            // For projects or workflows, fetch resource and compare owner
            // Uncomment and implement these as needed:
            if (route?.Contains("/projects/") == true)
            {
                var project = await _projectRepository.GetProjectByPuid(routePuid!);
                if (project != null && claimUserPuid != null && project.User_Id == claimUserId)
                        return true;
                return false;
            }
            // else if (httpContext.Request.Path.Value?.Contains("/workflows/") == true)
            // {
            //     var workflowRepo = httpContext.RequestServices.GetService(typeof(Business.Interfaces.IWorkflowRepository)) as Business.Interfaces.IWorkflowRepository;
            //     if (workflowRepo != null)
            //     {
            //         var workflow = await workflowRepo.GetByPuid(routepuid!);
            //         if (workflow != null && claimUserId != null && workflow.OwnerPuid == claimUserId)
            //             return true;
            //     }
            //     return false;
            // }

            // Default: not owner
            return false;
        }
         /// <summary>
        /// Checks if the user is the owner of the resource identified by puid in the route.
        /// </summary>
        public async Task<bool> IsAdmin(ClaimsPrincipal user)
        {
            // Must be authenticated
            if (!user.Identity?.IsAuthenticated ?? true)
                return false;

            // Check for Admin role claim
            var roleName = user.FindFirst(ClaimTypes.Role)?.Value;
            // Check if user is admin
            return roleName != null && roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }
        public async Task<ServiceResult<AuthResponse>> Login(string username, string password)
        {
            // Validate user credentials
            var userResult = await _repo.GetByUsername(username);
            if (userResult == null)
                return ServiceResult<AuthResponse>.Fail(ServiceStatus.Unauthorized401, "Invalid username or password.");

            var storedHash = await _repo.GetPasswordHash(userResult.User_Id);
            if (!PasswordHelper.VerifyPassword(password, storedHash))
                return ServiceResult<AuthResponse>.Fail(ServiceStatus.Unauthorized401, "Invalid username or password.");
            
            // Get claims for JWT
            var puid = userResult.Puid;
            var role = await _roleRepo.GetRole(userResult.Role_Id);
            if (role == null)
                return ServiceResult<AuthResponse>.Fail(ServiceStatus.InternalServerError500, $"User id {userResult.Role_Id} not found.");

            // Generate JWT
            var token = _jwtHelper.GenerateToken(userResult.User_Id, puid, role.Role_Name);
            return ServiceResult<AuthResponse>.SuccessResult(new AuthResponse { Puid = puid, Token = token });
        }
        public async Task<ServiceResult<AuthResponse>> RefreshToken(ClaimsPrincipal principal)
        {
            var puid = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleName = principal.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(puid) || string.IsNullOrEmpty(roleName))
                return ServiceResult<AuthResponse>.Fail(ServiceStatus.Unauthorized401, "Invalid token claims.");
            
            var userResult = await _repo.GetByPuid(puid);
            if (userResult == null)
                return ServiceResult<AuthResponse>.Fail(ServiceStatus.Unauthorized401, "User not found.");

            var newToken = _jwtHelper.GenerateToken(userResult.User_Id, puid, roleName);
            return ServiceResult<AuthResponse>.SuccessResult(new AuthResponse { Puid = puid, Token = newToken });
        }

        
    }
}
