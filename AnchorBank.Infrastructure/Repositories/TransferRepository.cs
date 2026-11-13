using AnchorBank.Application.Repositories.Interfaces;
using AnchorBank.Application.Transfers.Models;
using AnchorBank.Domain.Entities;
using AnchorBank.Domain.Enums;
using AnchorBank.Infrastructure.Database;
using Dapper;

namespace AnchorBank.Infrastructure.Repositories;

public class TransferRepository : ITransferRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public TransferRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<TransferResult> TransferAsync(
        Guid sourceAccountId,
        Guid customerId,
        string destinationAccountNumber,
        decimal amount,
        string? description)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var dbTransaction = connection.BeginTransaction();

        const string destinationIdSql = """
            SELECT Id FROM Accounts WHERE AccountNumber = @DestinationAccountNumber;
            """;

        var destinationId = await connection.ExecuteScalarAsync<Guid?>(
            destinationIdSql, new { DestinationAccountNumber = destinationAccountNumber }, dbTransaction);

        if (destinationId is null)
        {
            dbTransaction.Rollback();
            return new TransferResult(TransferOutcome.DestinationAccountNotFound);
        }

        if (destinationId == sourceAccountId)
        {
            dbTransaction.Rollback();
            return new TransferResult(TransferOutcome.SameAccount);
        }

        // Both rows are locked in Id order, so two transfers moving money in
        // opposite directions cannot deadlock on each other's account.
        const string selectSql = """
            SELECT Id, AccountNumber, Balance, AccountType,
                   AccountStatus, CustomerId, CreatedAt
            FROM Accounts WITH (UPDLOCK, ROWLOCK)
            WHERE Id IN (@SourceAccountId, @DestinationId)
            ORDER BY Id;
            """;

        var accounts = (await connection.QueryAsync<Account>(
            selectSql,
            new { SourceAccountId = sourceAccountId, DestinationId = destinationId.Value },
            dbTransaction)).ToDictionary(account => account.Id);

        // Ownership is checked against the row we just locked inside this transaction,
        // so it cannot go stale; someone else's account looks missing.
        if (!accounts.TryGetValue(sourceAccountId, out var source) || source.CustomerId != customerId)
        {
            dbTransaction.Rollback();
            return new TransferResult(TransferOutcome.SourceAccountNotFound);
        }

        if (!accounts.TryGetValue(destinationId.Value, out var destination))
        {
            dbTransaction.Rollback();
            return new TransferResult(TransferOutcome.DestinationAccountNotFound);
        }

        if (source.AccountStatus != AccountStatus.Active || destination.AccountStatus != AccountStatus.Active)
        {
            dbTransaction.Rollback();
            return new TransferResult(TransferOutcome.AccountInactive);
        }

        if (amount > source.Balance)
        {
            dbTransaction.Rollback();
            return new TransferResult(TransferOutcome.InsufficientFunds);
        }

        source.Withdraw(amount);
        destination.Deposit(amount);

        const string updateSql = """
            UPDATE Accounts
            SET Balance = @Balance
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(updateSql, new { source.Balance, source.Id }, dbTransaction);
        await connection.ExecuteAsync(updateSql, new { destination.Balance, destination.Id }, dbTransaction);

        var sourceTransaction = new Transaction(
            source.Id, TransactionType.Debit, TransactionCategory.Transfer, amount, description);

        var destinationTransaction = new Transaction(
            destination.Id, TransactionType.Credit, TransactionCategory.Transfer, amount, description);

        const string insertTransactionSql = """
            INSERT INTO Transactions
            (Id, AccountId, TransactionType, TransactionCategory, Amount, Description, CreatedAt)
            VALUES
            (@Id, @AccountId, @TransactionType, @TransactionCategory, @Amount, @Description, @CreatedAt);
            """;

        await connection.ExecuteAsync(
            insertTransactionSql,
            new[] { sourceTransaction, destinationTransaction },
            dbTransaction);

        var transfer = new Transfer(
            source.Id,
            destination.AccountNumber,
            string.Empty,
            amount,
            TransferType.Internal);

        transfer.Complete();

        const string insertTransferSql = """
            INSERT INTO Transfers
            (Id, SourceAccountId, DestinationAccountNumber, DestinationBankCode,
             Amount, TransferType, Status, CreatedAt)
            VALUES
            (@Id, @SourceAccountId, @DestinationAccountNumber, NULLIF(@DestinationBankCode, ''),
             @Amount, @TransferType, @Status, @CreatedAt);
            """;

        await connection.ExecuteAsync(insertTransferSql, transfer, dbTransaction);

        dbTransaction.Commit();

        return new TransferResult(
            TransferOutcome.Success,
            transfer.Id,
            source.Balance,
            destination.Balance);
    }
}
