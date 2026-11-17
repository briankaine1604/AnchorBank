using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AnchorBank.Api.Tests;

[Collection(ApiCollection.Name)]
public class AccountAuthorizationTests
{
    private readonly ApiFixture _fixture;

    public AccountAuthorizationTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RequestsWithoutToken_AreRejected()
    {
        var owner = await _fixture.Database.CreateCustomerAsync();
        var (accountId, _) = await _fixture.Database.CreateAccountAsync(owner, 100m);
        using var client = _fixture.Factory.CreateClient();

        var getAccount = await client.GetAsync($"/api/account/{accountId}");
        var getBalance = await client.GetAsync($"/api/account/{accountId}/balance");
        var deposit = await client.PostAsJsonAsync($"/api/account/{accountId}/deposit", new { amount = 10m });
        var create = await client.PostAsJsonAsync("/api/account", new { accountType = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, getAccount.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, getBalance.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, deposit.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task RequestsWithInvalidOrExpiredToken_AreRejected()
    {
        var owner = await _fixture.Database.CreateCustomerAsync();
        var (accountId, _) = await _fixture.Database.CreateAccountAsync(owner, 100m);
        using var client = _fixture.Factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");
        var invalid = await client.GetAsync($"/api/account/{accountId}/balance");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokens.Expired(owner));
        var expired = await client.GetAsync($"/api/account/{accountId}/balance");

        Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
    }

    [Fact]
    public async Task Customer_CanReadOwnAccount()
    {
        var owner = await _fixture.Database.CreateCustomerAsync();
        var (accountId, _) = await _fixture.Database.CreateAccountAsync(owner, 250m);
        using var client = ClientFor(owner);

        var balance = await client.GetAsync($"/api/account/{accountId}/balance");
        var account = await client.GetAsync($"/api/account/{accountId}");
        var accounts = await client.GetAsync("/api/account");

        Assert.Equal(HttpStatusCode.OK, balance.StatusCode);
        Assert.Equal(HttpStatusCode.OK, account.StatusCode);
        Assert.Equal(HttpStatusCode.OK, accounts.StatusCode);

        var body = await balance.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(accountId, body.GetProperty("accountId").GetGuid());
        Assert.Equal(250m, body.GetProperty("balance").GetDecimal());

        var list = await accounts.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(list.EnumerateArray(), item => item.GetProperty("id").GetGuid() == accountId);
    }

    [Fact]
    public async Task Customer_CannotReadAnotherCustomersAccount()
    {
        var owner = await _fixture.Database.CreateCustomerAsync();
        var (accountId, _) = await _fixture.Database.CreateAccountAsync(owner, 100m);
        var intruder = await _fixture.Database.CreateCustomerAsync();
        using var client = ClientFor(intruder);

        var account = await client.GetAsync($"/api/account/{accountId}");
        var balance = await client.GetAsync($"/api/account/{accountId}/balance");
        var transactions = await client.GetAsync($"/api/account/{accountId}/transactions");

        Assert.Equal(HttpStatusCode.NotFound, account.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, balance.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, transactions.StatusCode);
    }

    [Fact]
    public async Task Customer_CannotMoveMoneyFromAnotherCustomersAccount()
    {
        var owner = await _fixture.Database.CreateCustomerAsync();
        var (accountId, _) = await _fixture.Database.CreateAccountAsync(owner, 1000m);
        var destination = await _fixture.Database.CreateCustomerAsync();
        var (_, destinationNumber) = await _fixture.Database.CreateAccountAsync(destination, 0m);
        var intruder = await _fixture.Database.CreateCustomerAsync();
        using var client = ClientFor(intruder);

        var deposit = await client.PostAsJsonAsync(
            $"/api/account/{accountId}/deposit", new { amount = 100m });
        var withdraw = await client.PostAsJsonAsync(
            $"/api/account/{accountId}/withdraw", new { amount = 100m });
        var transfer = await client.PostAsJsonAsync(
            $"/api/account/{accountId}/transfers",
            new { destinationAccountNumber = destinationNumber, amount = 100m });

        Assert.Equal(HttpStatusCode.NotFound, deposit.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, withdraw.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, transfer.StatusCode);
        Assert.Equal(1000m, await _fixture.Database.GetBalanceAsync(accountId));
    }

    [Fact]
    public async Task Customer_CanTransferToAnotherCustomersAccount()
    {
        var sender = await _fixture.Database.CreateCustomerAsync();
        var (sourceId, _) = await _fixture.Database.CreateAccountAsync(sender, 1000m);
        var recipient = await _fixture.Database.CreateCustomerAsync();
        var (destinationId, destinationNumber) = await _fixture.Database.CreateAccountAsync(recipient, 500m);
        using var client = ClientFor(sender);

        var response = await client.PostAsJsonAsync(
            $"/api/account/{sourceId}/transfers",
            new { destinationAccountNumber = destinationNumber, amount = 200m });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(800m, await _fixture.Database.GetBalanceAsync(sourceId));
        Assert.Equal(700m, await _fixture.Database.GetBalanceAsync(destinationId));
    }

    [Fact]
    public async Task AccountCreation_UsesAuthenticatedCustomer()
    {
        var owner = await _fixture.Database.CreateCustomerAsync();
        var other = await _fixture.Database.CreateCustomerAsync();
        using var client = ClientFor(owner);

        // The body tries to create an account for someone else; the token must win.
        var response = await client.PostAsJsonAsync("/api/account", new { customerId = other, accountType = 1 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(owner, body.GetProperty("customerId").GetGuid());
    }

    [Fact]
    public async Task Customer_CanOnlyReadTheirOwnRecord()
    {
        var owner = await _fixture.Database.CreateCustomerAsync();
        var other = await _fixture.Database.CreateCustomerAsync();
        using var client = ClientFor(owner);

        var own = await client.GetAsync($"/api/customer/{owner}");
        var someoneElse = await client.GetAsync($"/api/customer/{other}");

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, someoneElse.StatusCode);
    }

    [Fact]
    public async Task UndefinedAccountType_ReturnsBadRequest()
    {
        var owner = await _fixture.Database.CreateCustomerAsync();
        using var client = ClientFor(owner);

        var response = await client.PostAsJsonAsync("/api/account", new { accountType = 99 });
        var accounts = await client.GetAsync("/api/account");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var list = await accounts.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Empty(list.EnumerateArray());
    }

    private HttpClient ClientFor(Guid customerId)
    {
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokens.For(customerId));

        return client;
    }
}
