using System.IdentityModel.Tokens.Jwt;
using GarageOS.Domain.Utils;
using System.Security.Claims;
using System.Text;
using GarageOS.Application.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace GarageOS.Api.Controllers;

/// <summary>Controller de autenticação JWT</summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    /// <summary>Inicializa o controller com configurações da aplicação</summary>
    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>Autentica o usuário administrativo e gera token JWT</summary>
    /// <remarks>
    /// Realiza a autenticação do usuário administrativo usando credenciais básicas (username e password).
    /// Após autenticação bem-sucedida, retorna um token JWT válido por 60 minutos.
    ///
    /// O token retornado deve ser enviado em todas as requisições para endpoints protegidos
    /// através do header Authorization: Bearer {token}
    ///
    /// Credenciais padrão:
    /// - Username: admin
    /// - Password: admin@123
    ///
    /// O token inclui:
    /// - Issuer: GarageOS.Api
    /// - Audience: GarageOS.Client
    /// - Expiração: 60 minutos a partir do login (GMT-3)
    /// - Claims: Nome do usuário e role "Admin"
    /// </remarks>
    /// <param name="request">Credenciais de acesso (Username, Password)</param>
    /// <returns>Token JWT válido para autenticação em endpoints protegidos</returns>
    /// <response code="200">Autenticação bem-sucedida - token JWT retornado</response>
    /// <response code="401">Credenciais inválidas (usuário ou senha incorretos)</response>
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
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(expires);
        var expiresAtBrasilia = BrasiliaTime.Agora.AddMinutes(expires);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, _configuration["Admin:Username"]!),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var jwtToken = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: creds);

        return new TokenResponse
        {
            Token     = new JwtSecurityTokenHandler().WriteToken(jwtToken),
            ExpiresAt = expiresAtBrasilia
        };
    }
}
