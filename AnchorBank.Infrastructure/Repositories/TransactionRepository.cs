using AnchorBank.Application.Repositories.Interfaces;
using AnchorBank.Application.Transactions.Models;
using AnchorBank.Domain.Entities;
using AnchorBank.Infrastructure.Database;
using Dapper;

namespace AnchorBank.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public TransactionRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<TransactionPage?> GetPageByAccountIdAsync(
        Guid accountId,
        Guid customerId,
        int page,
        int pageSize)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM Accounts
                                          WHERE Id = @AccountId AND CustomerId = @CustomerId)
                             THEN 1 ELSE 0 END AS BIT) AS AccountExists;

            SELECT COUNT(*) AS TotalCount
            FROM Transactions
            WHERE AccountId = @AccountId;

            SELECT Id, AccountId, TransactionType, TransactionCategory,
                   Amount, Description, CreatedAt
            FROM Transactions
            WHERE AccountId = @AccountId
            ORDER BY CreatedAt DESC, Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        using var results = await connection.QueryMultipleAsync(sql, new
        {
            AccountId = accountId,
            CustomerId = customerId,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });

        if (!await results.ReadSingleAsync<bool>())
            return null;

        var totalCount = await results.ReadSingleAsync<int>();
        var items = (await results.ReadAsync<Transaction>()).ToList();

        return new TransactionPage(page, pageSize, totalCount, items);
    }
}
