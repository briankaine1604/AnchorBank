using AnchorBank.Domain.Entities;
using AnchorBank.Infrastructure.Database;
using AnchorBank.Infrastructure.Repositories;
using AnchorBank.Infrastructure.Security;

namespace AnchorBank.Infrastructure.Tests;

[Collection(DatabaseCollection.Name)]
public class CustomerRepositoryTests
{
    private readonly DatabaseFixture _database;

    public CustomerRepositoryTests(DatabaseFixture database)
    {
        _database = database;
    }

    [Fact]
    public async Task RegisteredCustomer_StoresVerifiableHash()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var customer = new Customer(
            "Ada", "Lovelace", $"ada-{Guid.NewGuid()}@example.com", "08012345678", hasher.Hash("secret-password"));

        Assert.True(await NewRepository().TryCreateAsync(customer));
        var stored = await NewRepository().GetByIdAsync(customer.Id);

        Assert.NotNull(stored);
        Assert.NotEqual("secret-password", stored.PasswordHash);
        Assert.True(hasher.Verify("secret-password", stored.PasswordHash));
    }

    [Fact]
    public async Task CustomerWithoutPassword_RemainsReadable()
    {
        var customerId = await _database.InsertLegacyCustomerAsync();

        var stored = await NewRepository().GetByIdAsync(customerId);

        Assert.NotNull(stored);
        Assert.Null(stored.PasswordHash);
    }

    private CustomerRepository NewRepository()
    {
        return new CustomerRepository(new SqlConnectionFactory(_database.Configuration));
    }
}
