using Dapper;
using Microsoft.Data.SqlClient;

namespace AnchorBank.Api.Tests;

public sealed class ApiDatabaseFixture : IAsyncLifetime
{
    private const string DefaultConnection =
        "Server=localhost,1433;Database=master;User Id=sa;Password=Sugarmallow12345@;TrustServerCertificate=True;";

    private const string TestDatabase = "AnchorBankApiTests";

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var master = Environment.GetEnvironmentVariable("ANCHORBANK_TEST_CONNECTION") ?? DefaultConnection;

        await using (var connection = new SqlConnection(master))
        {
            await connection.OpenAsync();
            await connection.ExecuteAsync($"""
                IF DB_ID('{TestDatabase}') IS NOT NULL
                BEGIN
                    ALTER DATABASE [{TestDatabase}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{TestDatabase}];
                END
                CREATE DATABASE [{TestDatabase}];
                """);
        }

        ConnectionString = new SqlConnectionStringBuilder(master) { InitialCatalog = TestDatabase }.ConnectionString;

        await using (var connection = new SqlConnection(ConnectionString))
        {
            await connection.OpenAsync();

            foreach (var script in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Scripts"), "*.sql")
                         .Order(StringComparer.Ordinal))
            {
                await connection.ExecuteAsync(await File.ReadAllTextAsync(script));
            }
        }
    }

    public async Task DisposeAsync()
    {
        SqlConnection.ClearAllPools();

        await using var connection = new SqlConnection(
            new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = "master" }.ConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync($"""
            IF DB_ID('{TestDatabase}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{TestDatabase}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{TestDatabase}];
            END
            """);
    }

    public async Task<Guid> CreateCustomerAsync()
    {
        var customerId = Guid.NewGuid();

        await using var connection = new SqlConnection(ConnectionString);
        await connection.ExecuteAsync(
            """
            INSERT INTO Customers (Id, FirstName, LastName, Email, PhoneNumber, PasswordHash)
            VALUES (@Id, 'Test', 'Customer', @Email, '0800000000', NULL);
            """,
            new { Id = customerId, Email = $"{customerId}@example.com" });

        return customerId;
    }

    public async Task<(Guid Id, string AccountNumber)> CreateAccountAsync(Guid customerId, decimal balance)
    {
        var accountId = Guid.NewGuid();
        var accountNumber = accountId.ToString("N")[..20];

        await using var connection = new SqlConnection(ConnectionString);
        await connection.ExecuteAsync(
            """
            INSERT INTO Accounts (Id, AccountNumber, Balance, AccountType, AccountStatus, CustomerId, CreatedAt)
            VALUES (@Id, @AccountNumber, @Balance, 1, 1, @CustomerId, SYSUTCDATETIME());
            """,
            new { Id = accountId, AccountNumber = accountNumber, Balance = balance, CustomerId = customerId });

        return (accountId, accountNumber);
    }

    public async Task<decimal> GetBalanceAsync(Guid accountId)
    {
        await using var connection = new SqlConnection(ConnectionString);
        return await connection.ExecuteScalarAsync<decimal>(
            "SELECT Balance FROM Accounts WHERE Id = @Id;", new { Id = accountId });
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "api";
}
