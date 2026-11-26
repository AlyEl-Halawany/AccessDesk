using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using AccessDesk.Domain.Entities;
using AccessDesk.Infrastructure.Persistence;

namespace AccessDesk.Infrastructure.Services;

/// <summary>
/// Handles JWT access token creation and refresh token lifecycle.
///
/// Why two tokens?
/// - Access token: short-lived (15 min), stateless JWT. Fast to validate — no DB hit.
/// - Refresh token: long-lived, stored in DB. Used to get a new access token without
///   forcing the user to log in again. Stored in DB so we can revoke it (e.g., on logout).
/// </summary>
public class TokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AccessDeskDbContext _db;
    private readonly ILogger<TokenService> _logger;

    public TokenService(
        IOptions<JwtSettings> jwtSettings,
        UserManager<ApplicationUser> userManager,
        AccessDeskDbContext db,
        ILogger<TokenService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _userManager = userManager;
        _db = db;
        _logger = logger;
    }

    /// <summary>Creates a short-lived JWT for the given user, including their roles as claims.</summary>
    public async Task<string> CreateAccessTokenAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique token ID
        };

        // Add each role as a separate claim — this is how ASP.NET Core's [Authorize(Roles=...)] reads them
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Creates a new refresh token for the user and saves it to the database.
    /// Any previously active tokens for this user are left as-is (we support multiple devices).
    /// </summary>
    public async Task<RefreshToken> CreateRefreshTokenAsync(ApplicationUser user, CancellationToken ct = default)
    {
        // Generate a cryptographically random token value
        var tokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = tokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(ct);
        return refreshToken;
    }

    /// <summary>
    /// Validates an incoming refresh token, rotates it (creates a new one, revokes the old one),
    /// and returns a fresh access token + refresh token pair.
    /// </summary>
    public async Task<(string AccessToken, RefreshToken NewRefreshToken)?> RefreshAsync(
        string incomingToken, CancellationToken ct = default)
    {
        var stored = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == incomingToken, ct);

        if (stored is null || !stored.IsActive)
        {
            _logger.LogWarning("Invalid or expired refresh token used");
            return null;
        }

        // Rotate: revoke the old token and issue a new one
        stored.RevokedAt = DateTime.UtcNow;
        var newRefreshToken = await CreateRefreshTokenAsync(stored.User, ct);
        stored.ReplacedByToken = newRefreshToken.Token;
        await _db.SaveChangesAsync(ct);

        var accessToken = await CreateAccessTokenAsync(stored.User);
        return (accessToken, newRefreshToken);
    }

    /// <summary>Revoke a specific refresh token (called on logout).</summary>
    public async Task RevokeRefreshTokenAsync(string token, CancellationToken ct = default)
    {
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token, ct);
        if (stored is not null && stored.IsActive)
        {
            stored.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }
}

/// <summary>JWT configuration bound from appsettings.json.</summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>The secret key used to sign tokens. Must be at least 32 characters.</summary>
    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = "AccessDesk";
    public string Audience { get; set; } = "AccessDesk";

    /// <summary>How long an access token lives before the client must use the refresh token.</summary>
    public int AccessTokenExpiryMinutes { get; set; } = 15;

    /// <summary>How long a refresh token lives before the user must log in again.</summary>
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
