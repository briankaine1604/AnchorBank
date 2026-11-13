using AnchorBank.Application.Security;
using AnchorBank.Domain.Entities;
using AnchorBank.Infrastructure.Security;
using Microsoft.Extensions.Configuration;

namespace AnchorBank.Api.Tests;

public sealed class ApiFixture : IAsyncLifetime
{
    // Environment variables are read after user secrets, so they win over local
    // developer settings (which would otherwise point the API at the real database).
    private static readonly (string Name, string Value)[] Settings =
    [
        ("Jwt__SigningKey", ApiFactory.SigningKey),
        ("Jwt__Issuer", ApiFactory.Issuer),
        ("Jwt__Audience", ApiFactory.Audience),
        ("Jwt__ExpirationMinutes", "60"),
        ("RateLimiting__Registration__PermitLimit", "1000")
    ];

    public ApiDatabaseFixture Database { get; } = new();

    public ApiFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Database.InitializeAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", Database.ConnectionString);
        foreach (var (name, value) in Settings)
        {
            Environment.SetEnvironmentVariable(name, value);
        }

        Factory = new ApiFactory();

        // Start the host once so a configuration failure surfaces here, not in the first test.
        using var client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await Database.DisposeAsync();
    }
}

internal static class TestTokens
{
    private static readonly JwtAccessTokenGenerator Generator = CreateGenerator(60);
    private static readonly JwtAccessTokenGenerator ExpiredGenerator = CreateGenerator(-30);

    public static string For(Guid customerId) => Generate(Generator, customerId);

    public static string Expired(Guid customerId) => Generate(ExpiredGenerator, customerId);

    private static string Generate(JwtAccessTokenGenerator generator, Guid customerId)
    {
        var customer = new Customer(customerId, "Test", "Customer", $"{customerId}@example.com", "0800000000", null);

        return generator.Generate(customer).Value;
    }

    private static JwtAccessTokenGenerator CreateGenerator(int expirationMinutes)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = ApiFactory.SigningKey,
                ["Jwt:Issuer"] = ApiFactory.Issuer,
                ["Jwt:Audience"] = ApiFactory.Audience,
                ["Jwt:ExpirationMinutes"] = expirationMinutes.ToString()
            })
            .Build();

        return new JwtAccessTokenGenerator(configuration);
    }
}
