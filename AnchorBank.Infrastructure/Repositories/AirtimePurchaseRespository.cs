using AnchorBank.Application.Airtime;
using AnchorBank.Application.Airtime.Models;
using AnchorBank.Domain.Entities;
using AnchorBank.Domain.Enums;
using AnchorBank.Infrastructure.Database;
using Dapper;

namespace AnchorBank.Infrastructure.Repositories;

public class AirtimePurchaseRepository : IAirtimePurchaseRepository
{
    private const string LockAccountSql = """
        SELECT Id, AccountNumber, Balance, AccountType, AccountStatus, CustomerId, CreatedAt
        FROM Accounts WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @AccountId;
        """;

    private const string UpdateBalanceSql = "UPDATE Accounts SET Balance = @Balance WHERE Id = @Id;";

    private const string InsertTransactionSql = """
        INSERT INTO Transactions
        (Id, AccountId, TransactionType, TransactionCategory, Amount, Description, CreatedAt)
        VALUES
        (@Id, @AccountId, @TransactionType, @TransactionCategory, @Amount, @Description, @CreatedAt);
        """;

    private readonly SqlConnectionFactory _connectionFactory;

    public AirtimePurchaseRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<BeginAirtimePurchaseResult> BeginAsync(
        Guid accountId, Guid customerId, Network network,
        string phoneNumber, decimal amount, string requestId)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var dbTransaction = connection.BeginTransaction();

        var account = await connection.QuerySingleOrDefaultAsync<Account>(
            LockAccountSql, new { AccountId = accountId }, dbTransaction);

        // Someone else's account looks missing, same as in TransferRepository.
        if (account is null || account.CustomerId != customerId)
        {
            dbTransaction.Rollback();
            return new BeginAirtimePurchaseResult(BuyAirtimeOutcome.AccountNotFound);
        }

        if (account.AccountStatus != AccountStatus.Active)
        {
            dbTransaction.Rollback();
            return new BeginAirtimePurchaseResult(BuyAirtimeOutcome.AccountInactive);
        }

        if (amount > account.Balance)
        {
            dbTransaction.Rollback();
            return new BeginAirtimePurchaseResult(BuyAirtimeOutcome.InsufficientFunds);
        }

        account.Withdraw(amount);
        await connection.ExecuteAsync(UpdateBalanceSql, new { account.Balance, account.Id }, dbTransaction);

        var debit = new Transaction(
            account.Id, TransactionType.Debit, TransactionCategory.AirtimePurchase,
            amount, $"Airtime {network} {phoneNumber}");
        await connection.ExecuteAsync(InsertTransactionSql, debit, dbTransaction);

        var purchase = new AirtimePurchase(account.Id, network, phoneNumber, amount, requestId);

        const string insertPurchaseSql = """
            INSERT INTO AirtimePurchases
            (Id, AccountId, Network, PhoneNumber, Amount, RequestId, Status,
             ProviderReference, ProviderMessage, CreatedAt, UpdatedAt)
            VALUES
            (@Id, @AccountId, @Network, @PhoneNumber, @Amount, @RequestId, @Status,
             @ProviderReference, @ProviderMessage, @CreatedAt, @UpdatedAt);
            """;
        await connection.ExecuteAsync(insertPurchaseSql, purchase, dbTransaction);

        dbTransaction.Commit();
        return new BeginAirtimePurchaseResult(BuyAirtimeOutcome.Success, purchase, account.Balance);
    }

    public async Task SettleAsync(AirtimePurchase purchase)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            UPDATE AirtimePurchases
            SET Status = @Status, ProviderReference = @ProviderReference,
                ProviderMessage = @ProviderMessage, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(sql, purchase);
    }

    public async Task<decimal?> RefundAsync(AirtimePurchase purchase)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var dbTransaction = connection.BeginTransaction();

        // Only a purchase that is currently Failed in the database can be refunded.
        // If this updates 0 rows, someone already refunded it: credit nothing.
        const string markRefundedSql = """
            UPDATE AirtimePurchases
            SET Status = @Status, ProviderMessage = @ProviderMessage, UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND Status = @FailedStatus;
            """;

        var rows = await connection.ExecuteAsync(
            markRefundedSql,
            new
            {
                purchase.Status,
                purchase.ProviderMessage,
                purchase.UpdatedAt,
                purchase.Id,
                FailedStatus = AirtimePurchaseStatus.Failed
            },
            dbTransaction);

        if (rows != 1)
        {
            dbTransaction.Rollback();
            return null;
        }

        var account = await connection.QuerySingleAsync<Account>(
            LockAccountSql, new { AccountId = purchase.AccountId }, dbTransaction);

        account.Deposit(purchase.Amount);
        await connection.ExecuteAsync(UpdateBalanceSql, new { account.Balance, account.Id }, dbTransaction);

        var credit = new Transaction(
            account.Id, TransactionType.Credit, TransactionCategory.AirtimePurchase,
            purchase.Amount, $"Refund: airtime {purchase.Network} {purchase.PhoneNumber}");
        await connection.ExecuteAsync(InsertTransactionSql, credit, dbTransaction);

        dbTransaction.Commit();
        return account.Balance;
    }
}