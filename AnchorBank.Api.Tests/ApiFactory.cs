using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AnchorBank.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "api-tests-signing-key-that-is-long-enough-for-hmac";
    public const string Issuer = "AnchorBank";
    public const string Audience = "AnchorBankClient";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }
}
