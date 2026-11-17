using AnchorBank.Application.Accounts.Models;
using AnchorBank.Application.Repositories.Interfaces;
using AnchorBank.Domain.Entities;
using AnchorBank.Domain.Enums;
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

    public async Task<Account?> GetByIdAsync(Guid id, Guid customerId)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT Id, AccountNumber, Balance, AccountType,
                   AccountStatus, CustomerId, CreatedAt
            FROM Accounts
            WHERE Id = @Id AND CustomerId = @CustomerId;
            """;

        return await connection.QuerySingleOrDefaultAsync<Account>(
            sql, new { Id = id, CustomerId = customerId });
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

    public async Task<AccountMovementResult> ApplyMovementAsync(
        Guid accountId,
        Guid customerId,
        decimal amount,
        TransactionType transactionType,
        TransactionCategory transactionCategory,
        string? description)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var dbTransaction = connection.BeginTransaction();

        // UPDLOCK keeps this row locked until commit, so the balance read here
        // cannot change before the update below runs. The CustomerId predicate
        // makes someone else's account indistinguishable from a missing one.
        const string selectSql = """
            SELECT Id, AccountNumber, Balance, AccountType,
                   AccountStatus, CustomerId, CreatedAt
            FROM Accounts WITH (UPDLOCK, ROWLOCK)
            WHERE Id = @Id AND CustomerId = @CustomerId;
            """;

        var account = await connection.QuerySingleOrDefaultAsync<Account>(
            selectSql, new { Id = accountId, CustomerId = customerId }, dbTransaction);

        if (account is null)
        {
            dbTransaction.Rollback();
            return new AccountMovementResult(AccountMovementOutcome.AccountNotFound);
        }

        if (account.AccountStatus != AccountStatus.Active)
        {
            dbTransaction.Rollback();
            return new AccountMovementResult(AccountMovementOutcome.AccountInactive);
        }

        if (transactionType == TransactionType.Debit && amount > account.Balance)
        {
            dbTransaction.Rollback();
            return new AccountMovementResult(AccountMovementOutcome.InsufficientFunds);
        }

        if (transactionType == TransactionType.Credit)
            account.Deposit(amount);
        else
            account.Withdraw(amount);

        const string updateSql = """
            UPDATE Accounts
            SET Balance = @Balance
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(
            updateSql,
            new { account.Balance, account.Id },
            dbTransaction);

        var transaction = new Transaction(
            accountId,
            transactionType,
            transactionCategory,
            amount,
            description);

        const string insertSql = """
            INSERT INTO Transactions
            (Id, AccountId, TransactionType, TransactionCategory, Amount, Description, CreatedAt)
            VALUES
            (@Id, @AccountId, @TransactionType, @TransactionCategory, @Amount, @Description, @CreatedAt);
            """;

        await connection.ExecuteAsync(insertSql, transaction, dbTransaction);

        dbTransaction.Commit();

        return new AccountMovementResult(AccountMovementOutcome.Success, account.Balance);
    }
}
