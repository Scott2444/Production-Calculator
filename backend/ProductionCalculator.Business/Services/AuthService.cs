using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Helpers;
using System.Security.Claims;

namespace ProductionCalculator.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _repo;
        private readonly IRoleRepository _roleRepo;
        private readonly JwtHelper _jwtHelper;
        public AuthService(IUserRepository repo, IRoleRepository roleRepo, JwtHelper jwtHelper)
        {
            _repo = repo;
            _roleRepo = roleRepo;
            _jwtHelper = jwtHelper;
        }
        /// <summary>
        /// Checks if the user is the owner of the resource identified by pubId in the route.
        /// </summary>
        public async Task<bool> IsOwner(ClaimsPrincipal user, string? pubId, string? route)
        {
            // Must be authenticated
            if (!user.Identity?.IsAuthenticated ?? true)
                return false;

            // Get the claim from the JWT
            var claimUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // If accessing user resource, compare directly
            if (route?.Contains("/users/") == true)
            {
                return claimUserId != null && pubId == claimUserId;
            }

            // For projects or workflows, fetch resource and compare owner
            // Uncomment and implement these as needed:
            // if (httpContext.Request.Path.Value?.Contains("/projects/") == true)
            // {
            //     var projectRepo = httpContext.RequestServices.GetService(typeof(Business.Interfaces.IProjectRepository)) as Business.Interfaces.IProjectRepository;
            //     if (projectRepo != null)
            //     {
            //         var project = await projectRepo.GetByPuid(routePubId!);
            //         if (project != null && claimUserId != null && project.OwnerPuid == claimUserId)
            //             return true;
            //     }
            //     return false;
            // }
            // else if (httpContext.Request.Path.Value?.Contains("/workflows/") == true)
            // {
            //     var workflowRepo = httpContext.RequestServices.GetService(typeof(Business.Interfaces.IWorkflowRepository)) as Business.Interfaces.IWorkflowRepository;
            //     if (workflowRepo != null)
            //     {
            //         var workflow = await workflowRepo.GetByPuid(routePubId!);
            //         if (workflow != null && claimUserId != null && workflow.OwnerPuid == claimUserId)
            //             return true;
            //     }
            //     return false;
            // }

            // Default: not owner
            return false;
        }
         /// <summary>
        /// Checks if the user is the owner of the resource identified by pubId in the route.
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
            var pubId = userResult.Puid;
            var role = await _roleRepo.GetRole(userResult.Role_Id);
            if (role == null)
                return ServiceResult<AuthResponse>.Fail(ServiceStatus.InternalServerError500, $"User id {userResult.Role_Id} not found.");

            // Generate JWT
            var token = _jwtHelper.GenerateToken(pubId, role.Role_Name);
            return ServiceResult<AuthResponse>.SuccessResult(new AuthResponse { Puid = pubId, Token = token });
        }
        public async Task<ServiceResult<AuthResponse>> RefreshToken(ClaimsPrincipal principal)
        {
            var pubId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleName = principal.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(pubId) || string.IsNullOrEmpty(roleName))
                return ServiceResult<AuthResponse>.Fail(ServiceStatus.Unauthorized401, "Invalid token claims.");

            var newToken = _jwtHelper.GenerateToken(pubId, roleName);
            return ServiceResult<AuthResponse>.SuccessResult(new AuthResponse { Puid = pubId, Token = newToken });
        }

        
    }
}
