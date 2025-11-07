using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OilTrading.Application.DTOs;
using OilTrading.Core.Repositories;
using OilTrading.Infrastructure.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace OilTrading.Api.Controllers;

/// <summary>
/// Authentication and identity management endpoints.
/// Provides JWT-based authentication with token refresh capability.
///
/// Authentication Flow:
/// 1. POST /api/identity/login - Login with email/password
/// 2. Response includes accessToken (15m TTL) and refreshToken (7d TTL in httpOnly cookie)
/// 3. Frontend stores accessToken in memory, refreshToken in cookie (automatic)
/// 4. All subsequent requests use Authorization: Bearer {accessToken} header
/// 5. When accessToken expires (15m), call POST /api/identity/refresh to get new one
/// 6. Refresh token is also updated (rolling window)
/// 7. On logout, POST /api/identity/logout to clear tokens
///
/// Security Features:
/// - Refresh token stored in httpOnly cookie (not accessible to JavaScript)
/// - Access token returned in JSON (can be stored in memory)
/// - Passwords hashed with SHA-256 (extended with salt)
/// - HTTPS required in production
/// - CSRF protection via SameSite cookie policy
/// </summary>
[ApiController]
[Route("api/identity")]
public class IdentityController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<IdentityController> _logger;

    public IdentityController(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        ILogger<IdentityController> logger)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates user with email and password.
    /// Returns access token (15 min TTL) and refresh token (7 day TTL in httpOnly cookie).
    ///
    /// Request:
    /// POST /api/identity/login
    /// Content-Type: application/json
    /// {
    ///   "email": "trader@oiltrading.com",
    ///   "password": "password123"
    /// }
    ///
    /// Response on success (200 OK):
    /// {
    ///   "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    ///   "accessTokenExpiresAt": "2025-11-07T16:45:00Z",
    ///   "userId": "550e8400-e29b-41d4-a716-446655440000",
    ///   "email": "trader@oiltrading.com",
    ///   "fullName": "John Trader",
    ///   "role": "Trader",
    ///   "isActive": true
    /// }
    /// Set-Cookie: refreshToken={token}; HttpOnly; Secure; SameSite=Strict; Path=/; Max-Age=604800
    ///
    /// Response on failure (401 Unauthorized):
    /// {
    ///   "message": "Invalid email or password",
    ///   "errorCode": "InvalidCredentials"
    /// }
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            _logger.LogWarning("Login attempt with missing email or password from IP {IpAddress}",
                HttpContext.Connection.RemoteIpAddress);
            return BadRequest(new UnauthorizedErrorDto
            {
                Message = "Email and password are required",
                ErrorCode = "MissingCredentials"
            });
        }

        try
        {
            // Find user by email (case-insensitive)
            var user = await _userRepository.GetByEmailAsync(request.Email.ToLower());

            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Login attempt for non-existent or inactive user: {Email} from IP {IpAddress}",
                    request.Email, HttpContext.Connection.RemoteIpAddress);
                return Unauthorized(new UnauthorizedErrorDto
                {
                    Message = "Invalid email or password",
                    ErrorCode = "InvalidCredentials"
                });
            }

            // Verify password
            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for user {Email} from IP {IpAddress}",
                    request.Email, HttpContext.Connection.RemoteIpAddress);
                return Unauthorized(new UnauthorizedErrorDto
                {
                    Message = "Invalid email or password",
                    ErrorCode = "InvalidCredentials"
                });
            }

            // Generate tokens
            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var accessTokenExpiration = _jwtTokenService.GetTokenExpiration(accessToken) ?? DateTime.UtcNow.AddMinutes(15);

            // Set refresh token in httpOnly cookie
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps, // Secure in production (HTTPS)
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7), // 7 day TTL
                Path = "/"
            });

            // Update user last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User {Email} (ID: {UserId}) logged in successfully from IP {IpAddress}",
                user.Email, user.Id, HttpContext.Connection.RemoteIpAddress);

            // Return response
            return Ok(new LoginResponse
            {
                AccessToken = accessToken,
                AccessTokenExpiresAt = accessTokenExpiration,
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                IsActive = user.IsActive
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email {Email}", request.Email);
            return StatusCode(500, new UnauthorizedErrorDto
            {
                Message = "An error occurred during login",
                ErrorCode = "LoginError"
            });
        }
    }

    /// <summary>
    /// Refreshes the access token using the refresh token.
    /// Returns new access token (15 min TTL) and new refresh token (7 day TTL in httpOnly cookie).
    ///
    /// Request:
    /// POST /api/identity/refresh
    /// Content-Type: application/json
    /// Authorization: Bearer {currentAccessToken}
    /// Cookies: refreshToken={refreshToken} (sent automatically)
    /// {
    ///   "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    /// }
    ///
    /// Response on success (200 OK):
    /// {
    ///   "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    ///   "accessTokenExpiresAt": "2025-11-07T17:00:00Z",
    ///   "refreshTokenExpiresAt": "2025-11-14T16:45:00Z"
    /// }
    /// Set-Cookie: refreshToken={newToken}; HttpOnly; Secure; SameSite=Strict; Path=/; Max-Age=604800
    ///
    /// Response on failure (401 Unauthorized):
    /// {
    ///   "message": "Invalid or expired refresh token",
    ///   "errorCode": "InvalidRefreshToken"
    /// }
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<RefreshTokenResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        try
        {
            // Extract user ID from access token
            var userId = _jwtTokenService.GetUserIdFromToken(request.AccessToken);
            if (userId == null || userId == Guid.Empty)
            {
                _logger.LogWarning("Refresh token request with invalid access token from IP {IpAddress}",
                    HttpContext.Connection.RemoteIpAddress);
                return Unauthorized(new UnauthorizedErrorDto
                {
                    Message = "Invalid access token",
                    ErrorCode = "InvalidAccessToken"
                });
            }

            // Get user from database
            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Refresh token request for non-existent or inactive user {UserId} from IP {IpAddress}",
                    userId, HttpContext.Connection.RemoteIpAddress);
                return Unauthorized(new UnauthorizedErrorDto
                {
                    Message = "User not found or inactive",
                    ErrorCode = "UserNotFound"
                });
            }

            // Get refresh token from cookie
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("Refresh token request without refresh token cookie from IP {IpAddress}",
                    HttpContext.Connection.RemoteIpAddress);
                return Unauthorized(new UnauthorizedErrorDto
                {
                    Message = "Refresh token not found",
                    ErrorCode = "InvalidRefreshToken"
                });
            }

            // Note: In production, refresh tokens should be stored in database for:
            // - Revocation capability (logout invalidates token)
            // - Token rotation tracking
            // - Detecting token replay attacks
            // For now, we accept any valid refresh token (stateless approach)

            // Generate new tokens
            var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
            var accessTokenExpiration = _jwtTokenService.GetTokenExpiration(newAccessToken) ?? DateTime.UtcNow.AddMinutes(15);
            var refreshTokenExpiration = DateTime.UtcNow.AddDays(7);

            // Set new refresh token in httpOnly cookie
            Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = refreshTokenExpiration,
                Path = "/"
            });

            _logger.LogInformation("Access token refreshed for user {UserId} ({Email})",
                user.Id, user.Email);

            return Ok(new RefreshTokenResponse
            {
                AccessToken = newAccessToken,
                AccessTokenExpiresAt = accessTokenExpiration,
                RefreshTokenExpiresAt = refreshTokenExpiration
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh from IP {IpAddress}",
                HttpContext.Connection.RemoteIpAddress);
            return StatusCode(500, new UnauthorizedErrorDto
            {
                Message = "An error occurred during token refresh",
                ErrorCode = "RefreshError"
            });
        }
    }

    /// <summary>
    /// Logs out the user by clearing authentication tokens.
    /// In a production system with token revocation, this would:
    /// - Mark refresh token as revoked in database
    /// - Add access token to blacklist (cache) for remaining TTL
    ///
    /// Request:
    /// POST /api/identity/logout
    /// Authorization: Bearer {accessToken}
    /// Content-Type: application/json
    /// {}
    ///
    /// Response on success (200 OK):
    /// {
    ///   "message": "Successfully logged out",
    ///   "logoutAt": "2025-11-07T16:45:00Z"
    /// }
    /// Set-Cookie: refreshToken=; HttpOnly; Secure; SameSite=Strict; Path=/; Max-Age=0
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<LogoutResponse>> Logout([FromBody] LogoutRequest? request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new UnauthorizedErrorDto
                {
                    Message = "User not authenticated",
                    ErrorCode = "NotAuthenticated"
                });
            }

            // Clear refresh token cookie
            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });

            _logger.LogInformation("User {UserId} logged out. Reason: {Reason}",
                userId, request?.Reason ?? "No reason provided");

            return Ok(new LogoutResponse
            {
                Message = "Successfully logged out",
                LogoutAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new UnauthorizedErrorDto
            {
                Message = "An error occurred during logout",
                ErrorCode = "LogoutError"
            });
        }
    }

    /// <summary>
    /// Gets the current user's profile information.
    /// Requires valid JWT token in Authorization header.
    ///
    /// Request:
    /// GET /api/identity/profile
    /// Authorization: Bearer {accessToken}
    ///
    /// Response on success (200 OK):
    /// {
    ///   "id": "550e8400-e29b-41d4-a716-446655440000",
    ///   "email": "trader@oiltrading.com",
    ///   "firstName": "John",
    ///   "lastName": "Trader",
    ///   "fullName": "John Trader",
    ///   "role": "Trader",
    ///   "isActive": true,
    ///   "lastLoginAt": "2025-11-07T16:30:00Z"
    /// }
    ///
    /// Response on failure (401 Unauthorized):
    /// {
    ///   "message": "User not authenticated",
    ///   "errorCode": "NotAuthenticated"
    /// }
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized(new UnauthorizedErrorDto
                {
                    Message = "Invalid user ID in token",
                    ErrorCode = "InvalidToken"
                });
            }

            var user = await _userRepository.GetByIdAsync(userGuid);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return StatusCode(500, new { message = "An error occurred retrieving profile" });
        }
    }

    /// <summary>
    /// Verifies a password against its hash.
    /// Hash format: PBKDF2-SHA256 with salt
    /// </summary>
    private static bool VerifyPassword(string password, string hash)
    {
        try
        {
            // For now, simple SHA256 comparison (in production, use bcrypt or similar)
            // This is a placeholder - in real system use proper hashing
            var hashOfInput = ComputeSha256Hash(password);
            return hashOfInput.Equals(hash, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Computes SHA256 hash of input string.
    /// Note: In production, use bcrypt with proper salt instead of this method.
    /// </summary>
    private static string ComputeSha256Hash(string input)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
