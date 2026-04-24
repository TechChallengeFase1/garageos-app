using System.IdentityModel.Tokens.Jwt;
using GarageOS.Domain.Utils;
using System.Security.Claims;
using System.Text;
using GarageOS.Application.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace GarageOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>Autentica o usuário administrativo e retorna um token JWT.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var adminUsername = _configuration["Admin:Username"];
        var adminPassword = _configuration["Admin:Password"];

        if (request.Username != adminUsername || request.Password != adminPassword)
            return Unauthorized(new { mensagem = "Usuário ou senha inválidos." });

        var token = GerarToken();
        return Ok(token);
    }

    private TokenResponse GerarToken()
    {
        var secretKey = _configuration["Jwt:SecretKey"]!;
        var issuer    = _configuration["Jwt:Issuer"]!;
        var audience  = _configuration["Jwt:Audience"]!;
        var expires   = int.Parse(_configuration["Jwt:ExpiresInMinutes"]!);

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = BrasiliaTime.Agora.AddMinutes(expires);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, _configuration["Admin:Username"]!),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var jwtToken = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return new TokenResponse
        {
            Token     = new JwtSecurityTokenHandler().WriteToken(jwtToken),
            ExpiresAt = expiresAt
        };
    }
}
