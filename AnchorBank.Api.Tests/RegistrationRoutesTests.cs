using System.Net;
using System.Net.Http.Json;

namespace AnchorBank.Api.Tests;

[Collection(ApiCollection.Name)]
public class RegistrationRoutesTests
{
    private readonly ApiFixture _fixture;

    public RegistrationRoutesTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Registration_IsServedByAuthAndLeadsToLogin()
    {
        using var client = _fixture.Factory.CreateClient();
        var email = $"{Guid.NewGuid()}@example.com";

        var registration = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Ada",
            lastName = "Lovelace",
            email,
            phoneNumber = "08011112222",
            password = "registration-secret"
        });

        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var body = await registration.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "registration-secret"
        });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Registration_IsNoLongerServedByCustomerController()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/customer", new
        {
            firstName = "Ada",
            lastName = "Lovelace",
            email = $"{Guid.NewGuid()}@example.com",
            phoneNumber = "08011112222",
            password = "registration-secret"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
