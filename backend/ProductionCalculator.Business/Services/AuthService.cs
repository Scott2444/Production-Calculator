using ProductionCalculator.Business.APIModels;
using ProductionCalculator.Business.Interfaces;
using ProductionCalculator.Business.Models;
using ProductionCalculator.Business.Helpers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Resend;
using Resend.Payloads;

namespace ProductionCalculator.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserRepository _userRepo;
        private readonly IRoleRepository _roleRepo;
        private readonly IRefreshTokenRepository _refreshTokenRepo;
        private readonly JwtHelper _jwtHelper;
        private readonly RefreshTokenHelper _refreshTokenHelper;
        private readonly IProjectRepository _projectRepository;
        private readonly IVerificationCodeRepository _verificationCodeRepository;
        private readonly IResend _resend;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        public AuthService
        (
            ICurrentUserService currentUserService,
            IUserRepository userRepo, 
            IRoleRepository roleRepo, 
            IRefreshTokenRepository refreshTokenRepo,
            JwtHelper jwtHelper, 
            RefreshTokenHelper refreshTokenHelper,
            IProjectRepository projectRepository,
            IVerificationCodeRepository verificationCodeRepository,
            IResend resend,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            ILogger<AuthService> logger
        )
        {
            _currentUserService = currentUserService;
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _refreshTokenRepo = refreshTokenRepo;
            _jwtHelper = jwtHelper;
            _refreshTokenHelper = refreshTokenHelper;
            _projectRepository = projectRepository;
            _verificationCodeRepository = verificationCodeRepository;
            _resend = resend;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _logger = logger;
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
        public async Task<bool> IsOwner(ClaimsPrincipal userClaim)
        {
            // Must be authenticated
            if (!userClaim.Identity?.IsAuthenticated ?? true)
                return false;

            // Get the claim from the JWT
            var claimUserPuid = userClaim.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claimUserPuid))
                return false;
            var user = await _userRepo.GetByPuid(claimUserPuid);
            if (user == null)
                return false;
            

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
                if (project != null && claimUserPuid != null && project.User_Id == user.User_Id)
                        return true;
                return false;
            }

            // Default: not owner
            return false;
        }
         /// <summary>
        /// Checks if the user is the owner of the resource identified by puid in the route.
        /// </summary>
        public bool IsAdmin(ClaimsPrincipal userClaim)
        {
            // Must be authenticated
            if (!userClaim.Identity?.IsAuthenticated ?? true)
                return false;

            // Check for Admin role claim
            var roleName = userClaim.FindFirst(ClaimTypes.Role)?.Value;
            // Check if user is admin
            return roleName != null && roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        }
        public async Task<(ServiceResult<AuthResponse> result, string? accessToken, RefreshToken? refreshToken)> Login(string username, string password)
        {
            // Validate user credentials
            var user = await _userRepo.GetByUsername(username);
            if (user == null)
            {
                _logger.LogInformation("Login failure: User {Username} not found.", username);
                return (ServiceResult<AuthResponse>.Fail(ServiceStatus.Unauthorized401, "Invalid username or password."), null, null);
            }

            // Lockout check
            if (user.Lockout_Until != null && user.Lockout_Until > DateTime.UtcNow)
            {
                _logger.LogInformation("Login failure: User {Username} is locked out until {LockoutUntil}.", username, user.Lockout_Until);
                return (ServiceResult<AuthResponse>.Fail(ServiceStatus.Unauthorized401, "Invalid username or password."), null, null);
            }

            var storedHash = await _userRepo.GetPasswordHash(user.User_Id);
            if (!PasswordHelper.VerifyPassword(password, storedHash))
            {
                // Increment failed login attempts and possibly lockout
                LockoutHelper.UpdateUserLockout(_configuration, ref user);
                await _userRepo.UpdateUser(user);
                _logger.LogInformation("Login failure: Invalid password for user {Username}. Failed attempts: {FailedAttempts}.", username, user.Failed_Login_Attempts);
                return (ServiceResult<AuthResponse>.Fail(ServiceStatus.Unauthorized401, "Invalid username or password."), null, null);
            }
            
            // JWT Access Token
            // Get claims for JWT
            var puid = user.Puid;
            var role = await _roleRepo.GetRole(user.Role_Id);
            if (role == null)
            {
                _logger.LogError("Login failure: Role id {RoleId} not found for user {Username}.", user.Role_Id, username);
                return (ServiceResult<AuthResponse>.Fail(ServiceStatus.InternalServerError500, $"User id {user.Role_Id} not found."), null, null);
            }

            // Generate JWT
            var accessToken = _jwtHelper.GenerateToken(puid, role.Role_Name);

            // Refresh Token
            var refreshToken = await GenerateRefreshToken(user.User_Id);

            // Clear failed login attempts and lockout
            user.Failed_Login_Attempts = 0;
            user.Lockout_Until = null;
            await _userRepo.UpdateUser(user);

            _logger.LogInformation("Login success: User {Username} logged in.", username);

            return (ServiceResult<AuthResponse>.SuccessResult(new AuthResponse { Puid = puid, Username = user.Username }), accessToken, refreshToken);
        }
        public async Task<(ServiceResult<AuthResponse> result, string? accessToken)> RefreshToken(string? refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                return (ServiceResult<AuthResponse>.Fail(ServiceStatus.Unauthorized401, "Refresh token is required."), null);
            }

            // Validate refresh token
            var storedToken = await _refreshTokenRepo.GetRefreshTokenByToken(refreshToken);
            if (storedToken == null || storedToken.Expires_At <= DateTime.UtcNow || storedToken.Revoked_At != null)
            {
                _logger.LogInformation("Refresh token failure: Invalid or expired token {RefreshToken}.", refreshToken);
                return (ServiceResult<AuthResponse>.Fail(ServiceStatus.Unauthorized401, "Invalid or expired refresh token."), null);
            }

            // Get user info
            var user = await _userRepo.GetById(storedToken.User_Id);
            if (user == null)
            {
                _logger.LogInformation("Refresh token failure: User id {UserId} not found for token.", storedToken.User_Id);
                return (ServiceResult<AuthResponse>.Fail(ServiceStatus.Unauthorized401, "Invalid or expired refresh token."), null);
            }

            // Get role name for JWT
            var role = await _roleRepo.GetRole(user.Role_Id);
            if (role == null)
            {
                _logger.LogError("Refresh token failure: Invalid role {RoleId} for user {Username}.", user.Role_Id, user.Username);
                return (ServiceResult<AuthResponse>.Fail(ServiceStatus.InternalServerError500, "Invalid or expired refresh token."), null);
            }

            var newAccessToken = _jwtHelper.GenerateToken(user.Puid, role.Role_Name);
            _logger.LogInformation("Refresh token success: Access token issued for user {Username}.", user.Username);
            return (ServiceResult<AuthResponse>.SuccessResult(new AuthResponse { Puid = user.Puid, Username = user.Username }), newAccessToken);
        }

        public async Task<ServiceResult> RequestVerificationCode()
        {
            var verificationCodeSettings = _configuration.GetSection("VerificationCode");
            var maxRequests = verificationCodeSettings["MaxRequests"];
            var expireMinutes = verificationCodeSettings["ExpireMinutes"];
            if (maxRequests == null || expireMinutes == null)
            {
                _logger.LogError("RequestVerificationCode failure: Verification code parameters not configured.");
                throw new Exception("Verification code parameters not configured.");
            }
            int maxRequestsInt = int.Parse(maxRequests);
            int expireMinutesInt = int.Parse(expireMinutes);

            // Get current user from JWT
            var userPuid = _currentUserService.UserPuid;
            if (userPuid == null)
                return ServiceResult.SuccessResult();  // Should not happen, but avoid user enumeration
            var user = await _userRepo.GetByPuid(userPuid);
            if (user == null)
                return ServiceResult.SuccessResult(); 
            var userRole = await _roleRepo.GetRole(user.Role_Id);
            if (userRole == null || userRole.Role_Name != "Unverified")
                return ServiceResult.Fail(ServiceStatus.BadRequest400, "User is already verified.");  // User is already verified
            
            // Get previous codes
            var previousCodes = await _verificationCodeRepository.GetVerificationCodesByUserId(user.User_Id);
            previousCodes.Sort((a, b) => b.Created_At.CompareTo(a.Created_At));
            
            // Delete expired codes
            foreach (var previousCode in previousCodes.ToList())
            {
                if (previousCode.Expires_At <= DateTime.UtcNow)
                {
                    await _verificationCodeRepository.DeleteVerificationCode(previousCode.Code_Id);
                    previousCodes.Remove(previousCode);
                }
            }

            // Check if max requests exceeded
            if (previousCodes.Count >= maxRequestsInt)
            {
                return ServiceResult.Fail(ServiceStatus.TooManyRequests429, "Maximum verification code requests exceeded. Please try again later.");
            }

            // Generate code and hash
            var (code, codeHash) = VerificationCodeHelper.GenerateCode();

            // Create verification code entry
            var verificationCode = new VerificationCode
            {
                Code_Id = Guid.NewGuid(),
                User_Id = user.User_Id,
                Code_Hash = codeHash,
                Attempts = 0,  // Not used, but reserved in case of future need
                Created_At = DateTime.UtcNow,
                Expires_At = DateTime.UtcNow.AddMinutes(expireMinutesInt)
            };

            await _verificationCodeRepository.AddVerificationCode(verificationCode);

            // Send code via email using Resend
            try
            {
                await _resend.EmailSendAsync(VerificationCodeHelper.GenerateEmail(user.Email, code, expireMinutesInt.ToString()));
                _logger.LogInformation("Verification code sent to {Email} for user {Username}.", user.Email, user.Username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External service failure: Failed to send verification email to {Email} for user {Username}. Error: {Message}", user.Email, user.Username, ex.Message);
            }

            return ServiceResult.SuccessResult();
        }

        public async Task<ServiceResult> VerifyCode(string code)
        {
            // Get current user from JWT
            var userPuid = _currentUserService.UserPuid;
            if (userPuid == null)
            {
                _logger.LogInformation("VerifyCode failure: Unable to determine current user.");
                return ServiceResult.Fail(ServiceStatus.BadRequest400, "Invalid or expired verification code.");
            }
            var user = await _userRepo.GetByPuid(userPuid);
            if (user == null)
            {
                _logger.LogInformation("VerifyCode failure: User with PUID {UserPuid} not found.", userPuid);
                return ServiceResult.Fail(ServiceStatus.BadRequest400, "Invalid or expired verification code.");
            }

            // Get existing codes
            var existingCodes = await _verificationCodeRepository.GetVerificationCodesByUserId(user.User_Id);

            // Delete expired codes (keep db tidy)
            var expiredCodes = existingCodes.Where(pc => pc.Expires_At <= DateTime.UtcNow).ToList();
            foreach (var expiredCode in expiredCodes)
            {
                await _verificationCodeRepository.DeleteVerificationCode(expiredCode.Code_Id);
            }
            existingCodes = existingCodes.Except(expiredCodes).ToList();

            // Check for max attempts exceded on any non-expired codes - block any further attempts
            var verificationCodeSettings = _configuration.GetSection("VerificationCode")["MaxAttempts"];
            if (verificationCodeSettings == null)
            {
                _logger.LogError("VerifyCode failure: MaxAttempts not configured.");
                throw new Exception("Verification code parameters not configured.");
            }
            int maxAttemptsInt = int.Parse(verificationCodeSettings);
            foreach (var pc in existingCodes)
            {
                if (pc.Attempts >= maxAttemptsInt)
                {
                    _logger.LogInformation("VerifyCode failure: Max attempts exceeded for user {Username}.", user.Username);
                    return ServiceResult.Fail(ServiceStatus.TooManyRequests429, "Maximum verification code attempts exceeded. Please try again later.");
                }
            }

            // Check codes for a match
            var attemptedCode = existingCodes.FirstOrDefault(pc =>
            {
                return VerificationCodeHelper.VerifyCode(code, pc.Code_Hash);
            });
            if (attemptedCode == null)  // Invalid code, increment attempts on all non-expired codes
            {
                _logger.LogInformation("VerifyCode failure: Invalid code entered by user {Username}.", user.Username);
                foreach (var pc in existingCodes)
                {
                    pc.Attempts += 1;
                    await _verificationCodeRepository.UpdateVerificationCode(pc);
                }
                return ServiceResult.Fail(ServiceStatus.BadRequest400, "Invalid or expired verification code.");
            }

            // Valid code - set user to verified
            await SetUserToVerified(user.User_Id);
            _logger.LogInformation("Business state change: User {Username} successfully verified their email.", user.Username);

            return ServiceResult.SuccessResult();
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
        private async Task SetUserToVerified(int userId)
        {
            var userRole = await _roleRepo.GetRole("User");
            if (userRole == null)
            {
                _logger.LogError("SetUserToVerified failure: 'User' role not found in the system.");
                throw new Exception("User role not found.");
            }
            
            var user = await _userRepo.GetById(userId);
            if (user != null)
            {
                user.Role_Id = userRole.Role_Id;
                user.Last_Updated = DateTime.UtcNow;
                await _userRepo.UpdateUser(user);
            }
        }
    }
}
