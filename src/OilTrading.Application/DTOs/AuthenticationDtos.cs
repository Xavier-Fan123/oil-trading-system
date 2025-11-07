namespace OilTrading.Application.DTOs;

/// <summary>
/// Request DTO for user login.
/// Required fields: email, password
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User email address (unique identifier).
    /// Example: "trader@oiltrading.com"
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User password (plaintext - must be transmitted over HTTPS).
    /// Validation:
    /// - Required
    /// - Minimum 6 characters
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for successful login.
/// Contains access token and user information.
/// Refresh token is returned as httpOnly cookie (not in JSON).
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT access token (15 minute TTL).
    /// Format: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    /// Usage: Authorization header "Bearer {accessToken}"
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration time (UTC).
    /// Client should refresh token before this time.
    /// </summary>
    public DateTime AccessTokenExpiresAt { get; set; }

    /// <summary>
    /// User ID (GUID).
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User email.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User full name (FirstName LastName).
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User role (Trader, RiskManager, Administrator, Viewer).
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// User active status.
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Request DTO for token refresh.
/// Refresh token comes from httpOnly cookie (not in request body for security).
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Current access token (even if expired).
    /// Used to extract user ID for validation.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token from httpOnly cookie.
    /// Automatically extracted by middleware (not required in JSON body).
    /// </summary>
    public string? RefreshToken { get; set; }
}

/// <summary>
/// Response DTO for token refresh.
/// Contains new access token and updated expiration.
/// New refresh token is returned as httpOnly cookie.
/// </summary>
public class RefreshTokenResponse
{
    /// <summary>
    /// New JWT access token (15 minute TTL).
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// New access token expiration time (UTC).
    /// </summary>
    public DateTime AccessTokenExpiresAt { get; set; }

    /// <summary>
    /// Refresh token expiration time (UTC).
    /// Frontend should refresh again before this time.
    /// </summary>
    public DateTime RefreshTokenExpiresAt { get; set; }
}

/// <summary>
/// Request DTO for user logout.
/// No required fields - user is identified from JWT claim.
/// </summary>
public class LogoutRequest
{
    /// <summary>
    /// Optional reason for logout (audit trail).
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Response DTO for logout.
/// </summary>
public class LogoutResponse
{
    /// <summary>
    /// Confirmation message.
    /// </summary>
    public string Message { get; set; } = "Successfully logged out";

    /// <summary>
    /// Logout timestamp (UTC).
    /// </summary>
    public DateTime LogoutAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for user profile information.
/// Returned from /api/identity/profile endpoint.
/// </summary>
public class UserProfileDto
{
    /// <summary>
    /// User ID (GUID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User email (unique).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// First name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Full name (FirstName LastName).
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User role (Trader, RiskManager, Administrator, Viewer).
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// User active status.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Last login timestamp (UTC).
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// DTO for authorization error response.
/// </summary>
public class UnauthorizedErrorDto
{
    /// <summary>
    /// Error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error code (InvalidCredentials, TokenExpired, TokenInvalid, etc.).
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Additional details for debugging.
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }
}
