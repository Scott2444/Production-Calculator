using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Helpers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace ProductionCalculator.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IRoleRepository _roleRepo;
        private readonly IRefreshTokenRepository _refreshTokenRepo;
        private readonly JwtHelper _jwtHelper;
        private readonly RefreshTokenHelper _refreshTokenHelper;
        private readonly IProjectRepository _projectRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        public AuthService
        (
            IUserRepository userRepo, 
            IRoleRepository roleRepo, 
            IRefreshTokenRepository refreshTokenRepo,
            JwtHelper jwtHelper, 
            RefreshTokenHelper refreshTokenHelper,
            IProjectRepository projectRepository,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration
        )
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _refreshTokenRepo = refreshTokenRepo;
            _jwtHelper = jwtHelper;
            _refreshTokenHelper = refreshTokenHelper;
            _projectRepository = projectRepository;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }
        /// <summary>
        /// Checks if the resource identified by puid in the route is public.
        /// </summary>
        public async Task<bool> IsPublic()
        {
            // Extract puid from route
            var routeData = _httpContextAccessor.HttpContext?.GetRouteData();
            var projectPuid = routeData?.Values["projectPuid"]?.ToString();

            if (projectPuid == null) 
                return false;

            var project = await _projectRepository.GetProjectByPuid(projectPuid!);
            if (project == null)
                return false;

            return project.Is_Public;
        }
        /// <summary>
        /// Checks if the user is the owner of the resource identified by puid in the route.
        /// </summary>
        public async Task<bool> IsOwner(ClaimsPrincipal user)
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

            // Extract puid from route
            var routeData = _httpContextAccessor.HttpContext?.GetRouteData();
            var userPuid = routeData?.Values["userPuid"]?.ToString();
            var projectPuid = routeData?.Values["projectPuid"]?.ToString();

            // If accessing user resource, compare directly
            if (userPuid != null)
            {
                return claimUserPuid != null && userPuid == claimUserPuid;
            }

            // For projects or workflows, fetch resource and compare owner
            // Uncomment and implement these as needed:
            if (projectPuid != null)
            {
                var project = await _projectRepository.GetProjectByPuid(projectPuid!);
                if (project != null && claimUserPuid != null && project.User_Id == claimUserId)
                        return true;
                return false;
            }

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
        public async Task<(ServiceResult<AuthResponse> result, string? accessToken, RefreshToken? refreshToken)> Login(string username, string password)
        {
            // Validate user credentials
            var user = await _userRepo.GetByUsername(username);
            if (user == null)
                return (ServiceResult<AuthResponse>.Fail(ServiceStatus.Unauthorized401, "Invalid username or password."), null, null);

            var storedHash = await _userRepo.GetPasswordHash(user.User_Id);
            if (!PasswordHelper.VerifyPassword(password, storedHash))
                return (ServiceResult<AuthResponse>.Fail(ServiceStatus.Unauthorized401, "Invalid username or password."), null, null);
            
            // JWT Access Token
            // Get claims for JWT
            var puid = user.Puid;
            var role = await _roleRepo.GetRole(user.Role_Id);
            if (role == null)
                return (ServiceResult<AuthResponse>.Fail(ServiceStatus.InternalServerError500, $"User id {user.Role_Id} not found."), null, null);

            // Generate JWT
            var accessToken = _jwtHelper.GenerateToken(user.User_Id, puid, role.Role_Name);

            // Refresh Token
            var refreshToken = await GenerateRefreshToken(user.User_Id);

            return (ServiceResult<AuthResponse>.SuccessResult(new AuthResponse { Puid = puid }), accessToken, refreshToken);
        }
        public async Task<(ServiceResult<AuthResponse> result, string? accessToken)> RefreshToken(string? refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                return (ServiceResult<AuthResponse>.Fail(ServiceStatus.BadRequest400, "Refresh token is required."), null);

            // Validate refresh token
            var storedToken = await _refreshTokenRepo.GetRefreshTokenByToken(refreshToken);
            if (storedToken == null || storedToken.Expires_At <= DateTime.UtcNow || storedToken.Revoked_At != null)
                return (ServiceResult<AuthResponse>.Fail(ServiceStatus.Unauthorized401, "Invalid or expired refresh token."), null);

            // Get user info
            var user = await _userRepo.GetById(storedToken.User_Id);
            if (user == null)
                return (ServiceResult<AuthResponse>.Fail(ServiceStatus.Unauthorized401, "User not found."), null);

            // Get role name for JWT
            var role = await _roleRepo.GetRole(user.Role_Id);
            if (role == null)
                return (ServiceResult<AuthResponse>.Fail(ServiceStatus.InternalServerError500, "Invalid role."), null);

            var newAccessToken = _jwtHelper.GenerateToken(user.User_Id, user.Puid, role.Role_Name);
            return (ServiceResult<AuthResponse>.SuccessResult(new AuthResponse { Puid = user.Puid }), newAccessToken);
        }

        private async Task<RefreshToken?> GenerateRefreshToken(int userId)
        {
            var maxSessions = _configuration.GetSection("RefreshToken")["MaxSessions"];
            int maxSessionsInt = int.Parse(maxSessions ?? "5");

            // Get existing tokens
            var existingTokens = await _refreshTokenRepo.GetRefreshTokensByUserId(userId);
            while (existingTokens.Count >= maxSessionsInt)
            {
                // Revoke oldest token
                existingTokens.Sort((a, b) => b.Created_At.CompareTo(a.Created_At));
                var oldestToken = existingTokens.Last();
                await _refreshTokenRepo.DeleteRefreshToken(oldestToken.Token_Id);
                existingTokens.RemoveAt(existingTokens.Count - 1);
            }
            
            // Create new token
            var refreshToken = _refreshTokenHelper.AssembleRefreshToken(userId);
            await _refreshTokenRepo.AddRefreshToken(refreshToken);
            return refreshToken;
        }
    }
}
