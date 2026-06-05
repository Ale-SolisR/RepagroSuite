using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Rastreo.Api.Models;

namespace Rastreo.Api.Services;

public class JwtService
{
    private readonly IConfiguration _cfg;

    public JwtService(IConfiguration cfg) { _cfg = cfg; }

    public int ExpiryMinutes => int.Parse(_cfg["Rastreo:Jwt:ExpiryMinutes"] ?? "480"); // 8h default

    /// <summary>Genera el JWT. <paramref name="sesionToken"/> queda en el claim 'sid' para forzar sesión única.</summary>
    public (string token, DateTime expira) GenerarToken(Usuario u, Guid sesionToken)
    {
        var key = _cfg["Rastreo:Jwt:Key"] ?? throw new InvalidOperationException("Rastreo:Jwt:Key no configurado");
        var issuer = _cfg["Rastreo:Jwt:Issuer"] ?? "Rastreo.Api";
        var audience = _cfg["Rastreo:Jwt:Audience"] ?? "Rastreo.Client";

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, u.Id.ToString()),
            new Claim("uid", u.Id.ToString()),
            new Claim("rol", u.Rol),
            new Claim("sid", sesionToken.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, u.Correo),
            new Claim(ClaimTypes.Name, u.Nombre ?? u.Correo),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expira = DateTime.UtcNow.AddMinutes(ExpiryMinutes);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expira, signingCredentials: creds);
        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}
