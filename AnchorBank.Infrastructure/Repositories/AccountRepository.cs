using AnchorBank.Application.Repositories.Interfaces;
using AnchorBank.Domain.Entities;
using AnchorBank.Infrastructure.Database;
using Dapper;

namespace AnchorBank.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public AccountRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task CreateAsync(Account account)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            INSERT INTO Accounts
            (Id, AccountNumber, Balance, AccountType, AccountStatus, CustomerId, CreatedAt)
            VALUES
            (@Id, @AccountNumber, @Balance, @AccountType, @AccountStatus, @CustomerId, @CreatedAt);
            """;

        await connection.ExecuteAsync(sql, account);
    }

    public async Task<Account?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT Id, AccountNumber, Balance, AccountType,
                   AccountStatus, CustomerId, CreatedAt
            FROM Accounts
            WHERE Id = @Id;
            """;

        return await connection.QuerySingleOrDefaultAsync<Account>(
            sql, new { Id = id });
    }

    public async Task<IEnumerable<Account>> GetByCustomerIdAsync(Guid customerId)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT Id, AccountNumber, Balance, AccountType,
                   AccountStatus, CustomerId, CreatedAt
            FROM Accounts
            WHERE CustomerId = @CustomerId;
            """;

        return await connection.QueryAsync<Account>(
            sql, new { CustomerId = customerId });
    }

    public async Task<long> GetNextAccountNumberAsync()
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT NEXT VALUE FOR AccountNumberSequence;
            """;

        return await connection.ExecuteScalarAsync<long>(sql);
    }
}
