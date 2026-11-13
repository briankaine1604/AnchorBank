using System.Security.Claims;
using System.Text;
using AnchorBank.Application.Security;
using AnchorBank.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AnchorBank.Infrastructure.Security;

public class JwtAccessTokenGenerator : IAccessTokenGenerator
{
    private readonly string _signingKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public JwtAccessTokenGenerator(IConfiguration configuration)
    {
        _signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException(
                "Jwt:SigningKey is missing. Set it with user-secrets or the Jwt__SigningKey environment variable.");
        _issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is missing.");
        _audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is missing.");
        _expirationMinutes = int.TryParse(configuration["Jwt:ExpirationMinutes"], out var minutes) ? minutes : 60;
    }

    public AccessToken Generate(Customer customer)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_expirationMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _issuer,
            Audience = _audience,
            Expires = expiresAt,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", customer.Id.ToString()),
                new Claim("email", customer.Email)
            }),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}
