using AnchorBank.Domain.Entities;
using AnchorBank.Domain.Enums;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AnchorBank.Infrastructure.Tests;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string DefaultConnection =
        "Server=localhost,1433;Database=master;User Id=sa;Password=Sugarmallow12345@;TrustServerCertificate=True;";

    private const string TestDatabase = "AnchorBankTests";

    public string ConnectionString { get; private set; } = string.Empty;

    public IConfiguration Configuration { get; private set; } = null!;

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

            var scripts = Path.Combine(AppContext.BaseDirectory, "Scripts");
            foreach (var script in Directory.GetFiles(scripts, "*.sql").Order(StringComparer.Ordinal))
            {
                await connection.ExecuteAsync(await File.ReadAllTextAsync(script));
            }
        }

        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString
            })
            .Build();
    }

    public async Task DisposeAsync()
    {
        SqlConnection.ClearAllPools();

        await using var connection = new SqlConnection(new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = "master" }.ConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync($"""
            IF DB_ID('{TestDatabase}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{TestDatabase}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{TestDatabase}];
            END
            """);
    }

    public async Task<Guid> CreateAccountAsync(decimal balance, AccountStatus status = AccountStatus.Active)
    {
        var customerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync(
            "INSERT INTO Customers (Id, FirstName, LastName, Email, PhoneNumber) VALUES (@Id, 'Test', 'User', @Email, '0000000000');",
            new { Id = customerId, Email = $"{customerId}@example.com" });

        await connection.ExecuteAsync(
            """
            INSERT INTO Accounts (Id, AccountNumber, Balance, AccountType, AccountStatus, CustomerId, CreatedAt)
            VALUES (@Id, @AccountNumber, @Balance, @AccountType, @AccountStatus, @CustomerId, SYSUTCDATETIME());
            """,
            new
            {
                Id = accountId,
                AccountNumber = accountId.ToString("N")[..20],
                Balance = balance,
                AccountType = AccountType.Current,
                AccountStatus = status,
                CustomerId = customerId
            });

        return accountId;
    }

    public async Task<decimal> GetBalanceAsync(Guid accountId)
    {
        await using var connection = new SqlConnection(ConnectionString);
        return await connection.ExecuteScalarAsync<decimal>(
            "SELECT Balance FROM Accounts WHERE Id = @Id;", new { Id = accountId });
    }

    public async Task<string> GetAccountNumberAsync(Guid accountId)
    {
        await using var connection = new SqlConnection(ConnectionString);
        return await connection.ExecuteScalarAsync<string>(
            "SELECT AccountNumber FROM Accounts WHERE Id = @Id;", new { Id = accountId });
    }

    public async Task<IReadOnlyList<TransferRow>> GetTransfersAsync(Guid sourceAccountId)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var transfers = await connection.QueryAsync<TransferRow>(
            """
            SELECT Id, SourceAccountId, DestinationAccountNumber, DestinationBankCode,
                   Amount, TransferType, Status, CreatedAt
            FROM Transfers
            WHERE SourceAccountId = @SourceAccountId;
            """,
            new { SourceAccountId = sourceAccountId });

        return transfers.ToList();
    }

    public async Task<IReadOnlyList<Transaction>> GetTransactionsAsync(Guid accountId)
    {
        await using var connection = new SqlConnection(ConnectionString);
        var transactions = await connection.QueryAsync<Transaction>(
            """
            SELECT Id, AccountId, TransactionType, TransactionCategory, Amount, Description, CreatedAt
            FROM Transactions
            WHERE AccountId = @AccountId
            ORDER BY CreatedAt;
            """,
            new { AccountId = accountId });

        return transactions.ToList();
    }
}

[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "database";
}

public record TransferRow(
    Guid Id,
    Guid SourceAccountId,
    string DestinationAccountNumber,
    string? DestinationBankCode,
    decimal Amount,
    TransferType TransferType,
    TransferStatus Status,
    DateTime CreatedAt);
