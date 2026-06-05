using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Domain.Entities;

namespace RepagroSuite.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("fullName", user.FullName),
        };

        // Sello de sesión: ata el token a la última sesión iniciada (sesión única). Si el usuario
        // vuelve a iniciar sesión, LastLoginAt cambia y los tokens anteriores dejan de ser válidos.
        if (user.LastLoginAt.HasValue)
            claims.Add(new Claim("lat", user.LastLoginAt.Value.Ticks.ToString()));

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        // Expiración por inactividad: el refresh token es deslizante (se rota en cada refresh).
        // Si el usuario no tiene actividad durante este lapso, el token caduca y la sesión se cierra.
        var refreshHours = int.Parse(_config["Jwt:RefreshTokenExpiryHours"] ?? "3");

        return new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(randomBytes),
            ExpiresAt = DateTime.UtcNow.AddHours(refreshHours),
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }

    public Guid? GetUserIdFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _config["Jwt:Audience"],
                ValidateLifetime = false
            }, out var validatedToken);

            var jwt = (JwtSecurityToken)validatedToken;
            var id = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(id, out var guid) ? guid : null;
        }
        catch
        {
            return null;
        }
    }
}
