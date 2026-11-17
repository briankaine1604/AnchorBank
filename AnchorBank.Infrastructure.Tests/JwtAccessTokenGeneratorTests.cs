using System.Text;
using AnchorBank.Domain.Entities;
using AnchorBank.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AnchorBank.Infrastructure.Tests;

public class JwtAccessTokenGeneratorTests
{
    private const string SigningKey = "test-signing-key-with-more-than-32-characters";
    private const string Issuer = "AnchorBank";
    private const string Audience = "AnchorBankClient";

    [Fact]
    public async Task Generate_ProducesSignedTokenWithCustomerClaimsAndExpiry()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = SigningKey,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:ExpirationMinutes"] = "30"
            })
            .Build();

        var customer = new Customer(
            "Ada", "Lovelace", "ada@example.com", "08012345678", "PBKDF2-SHA256$600000$c2FsdA==$aGFzaA==");

        var token = new JwtAccessTokenGenerator(configuration).Generate(customer);

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            ValidateLifetime = true
        });

        Assert.True(validation.IsValid);
        Assert.Equal(customer.Id.ToString(), validation.Claims["sub"]);
        Assert.Equal("ada@example.com", validation.Claims["email"]);
        Assert.InRange(token.ExpiresAt, DateTime.UtcNow.AddMinutes(29), DateTime.UtcNow.AddMinutes(31));
    }
}
