using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AnchorBank.Api.Tests;

[Collection(ApiCollection.Name)]
public class RegistrationValidationTests
{
    private readonly ApiFixture _fixture;

    public RegistrationValidationTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DuplicateEmail_ReturnsConflict()
    {
        using var client = _fixture.Factory.CreateClient();
        var registration = NewRegistration();

        var first = await client.PostAsJsonAsync("/api/auth/register", registration);
        var second = await client.PostAsJsonAsync("/api/auth/register", registration);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task ConcurrentDuplicateRegistrations_ProduceOneSuccessAndOneConflict()
    {
        using var client = _fixture.Factory.CreateClient();
        var registration = NewRegistration();

        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/auth/register", registration),
            client.PostAsJsonAsync("/api/auth/register", registration));

        var statuses = responses.Select(response => response.StatusCode).OrderBy(status => status).ToArray();

        Assert.Equal(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }, statuses);
    }

    [Theory]
    [InlineData("firstName", 101)]
    [InlineData("lastName", 101)]
    [InlineData("email", 256)]
    [InlineData("phoneNumber", 21)]
    public async Task OversizedFields_ReturnBadRequest(string field, int length)
    {
        using var client = _fixture.Factory.CreateClient();
        var registration = NewRegistration();
        registration[field] = new string('x', length);

        var response = await client.PostAsJsonAsync("/api/auth/register", registration);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("ada@")]
    public async Task MalformedEmail_ReturnsBadRequest(string email)
    {
        using var client = _fixture.Factory.CreateClient();
        var registration = NewRegistration();
        registration["email"] = email;

        var response = await client.PostAsJsonAsync("/api/auth/register", registration);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Registration_Returns201WithoutLocationHeader()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewRegistration());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(Guid.Empty, body.GetProperty("id").GetGuid());
    }

    private static Dictionary<string, string> NewRegistration()
    {
        return new Dictionary<string, string>
        {
            ["firstName"] = "Ada",
            ["lastName"] = "Lovelace",
            ["email"] = $"ada-{Guid.NewGuid()}@example.com",
            ["phoneNumber"] = "08011112222",
            ["password"] = "registration-secret"
        };
    }
}
