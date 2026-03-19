using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TradingAnalytics.Shared.Kernel;

namespace TradingAnalytics.Shared.Infrastructure.Auth;

/// <summary>
/// Generates signed JWT tokens.
/// </summary>
public sealed class JwtService(IOptions<JwtConfig> config) : IJwtService
{
    private readonly JwtConfig _config = config?.Value ?? throw new ArgumentNullException(nameof(config));

    /// <inheritdoc />
    public string GenerateCustomerToken(Guid customerId, string? email, string? phone, string role)
    {
        var claims = new List<Claim>
        {
            new(Constants.ClaimTypes.UserId, customerId.ToString()),
            new(Constants.ClaimTypes.ActorType, Constants.ActorTypes.Customer),
            new(Constants.ClaimTypes.Role, role),
        };

        if (email is not null)
        {
            claims.Add(new Claim(Constants.ClaimTypes.Email, email));
        }

        if (phone is not null)
        {
            claims.Add(new Claim(Constants.ClaimTypes.Phone, phone));
        }

        return Build(claims);
    }

    /// <inheritdoc />
    public string GenerateAdminToken(Guid adminId, string email, string role) =>
        Build(
            [
                new Claim(Constants.ClaimTypes.UserId, adminId.ToString()),
                new Claim(Constants.ClaimTypes.ActorType, Constants.ActorTypes.Admin),
                new Claim(Constants.ClaimTypes.Role, role),
                new Claim(Constants.ClaimTypes.Email, email),
            ]);

    private string Build(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.Secret));
        var token = new JwtSecurityToken(
            issuer: _config.Issuer,
            audience: _config.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_config.ExpiryMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
