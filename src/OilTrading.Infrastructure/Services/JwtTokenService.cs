using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OilTrading.Core.Entities;

namespace OilTrading.Infrastructure.Services;

/// <summary>
/// Service for JWT token generation, validation, and refresh token management.
/// Implements stateless authentication with automatic token refresh capability.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT access token for the specified user.
    /// Token TTL: 15 minutes (configurable via appsettings.json)
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generates a refresh token for the specified user.
    /// Token TTL: 7 days (configurable via appsettings.json)
    /// Stored as httpOnly cookie on client.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT token and returns claims if valid.
    /// Returns null if token is invalid or expired.
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Gets the token expiration time from a valid token.
    /// </summary>
    DateTime? GetTokenExpiration(string token);

    /// <summary>
    /// Gets the user ID from a valid token's claims.
    /// </summary>
    Guid? GetUserIdFromToken(string token);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;

        var jwtSection = _configuration.GetSection("Jwt");
        _secretKey = jwtSection["SecretKey"] ?? throw new InvalidOperationException("JWT:SecretKey is not configured");
        _issuer = jwtSection["Issuer"] ?? "https://localhost:5000";
        _audience = jwtSection["Audience"] ?? "https://localhost:5000";

        _accessTokenExpirationMinutes = int.TryParse(
            jwtSection["ExpirationMinutes"],
            out var minutes) ? minutes : 15; // Default: 15 minutes

        _refreshTokenExpirationDays = int.TryParse(
            jwtSection["RefreshTokenExpirationDays"],
            out var days) ? days : 7; // Default: 7 days

        // Validate secret key length (must be at least 256 bits / 32 bytes for HS256)
        if (_secretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT:SecretKey must be at least 32 characters long (256 bits) for HS256");
        }
    }

    /// <summary>
    /// Generates a JWT access token with user claims.
    /// Claims included:
    /// - sub: User ID (GUID)
    /// - email: User email
    /// - name: User full name
    /// - role: User role (Trader, RiskManager, Administrator, Viewer)
    /// - iat: Issued at timestamp
    /// - exp: Expiration timestamp
    /// </summary>
    public string GenerateAccessToken(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("IsActive", user.IsActive.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a cryptographically secure refresh token.
    /// Token format: URL-safe base64 string (256 bits of random data)
    /// Note: In production, refresh tokens should be persisted in database
    /// with user association for revocation capability.
    /// </summary>
    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }
    }

    /// <summary>
    /// Validates a JWT token and returns the claims principal if valid.
    /// Checks:
    /// - Token signature (HS256)
    /// - Token expiration
    /// - Issuer
    /// - Audience
    /// Returns null if any validation fails.
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var tokenHandler = new JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(5), // 5 second grace period
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch (Exception)
        {
            // Token validation failed - return null
            // Specific exceptions: SecurityTokenInvalidSignatureException,
            // SecurityTokenExpiredException, SecurityTokenInvalidIssuerException, etc.
            return null;
        }
    }

    /// <summary>
    /// Extracts the expiration time from a JWT token.
    /// Returns null if token is invalid or doesn't contain exp claim.
    /// </summary>
    public DateTime? GetTokenExpiration(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp);
            if (expClaim == null)
                return null;

            if (long.TryParse(expClaim.Value, out var unixTimestamp))
            {
                return UnixTimeStampToDateTime(unixTimestamp);
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts the user ID from a JWT token's subject claim.
    /// Returns null if token is invalid or doesn't contain sub claim.
    /// </summary>
    public Guid? GetUserIdFromToken(string token)
    {
        try
        {
            var principal = ValidateToken(token);
            if (principal == null)
                return null;

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return null;

            return userId;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Converts Unix timestamp (seconds since epoch) to DateTime UTC.
    /// </summary>
    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
        return dateTime;
    }
}
