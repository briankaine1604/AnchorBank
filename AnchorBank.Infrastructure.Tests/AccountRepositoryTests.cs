using AnchorBank.Application.Accounts.Models;
using AnchorBank.Domain.Enums;
using AnchorBank.Infrastructure.Database;
using AnchorBank.Infrastructure.Repositories;

namespace AnchorBank.Infrastructure.Tests;

[Collection(DatabaseCollection.Name)]
public class AccountRepositoryTests
{
    private readonly DatabaseFixture _database;

    public AccountRepositoryTests(DatabaseFixture database)
    {
        _database = database;
    }

    [Fact]
    public async Task Deposit_UpdatesBalanceAndRecordsTransaction()
    {
        var accountId = await _database.CreateAccountAsync(balance: 100m);

        var result = await NewRepository().ApplyMovementAsync(
            accountId, 50m, TransactionType.Credit, TransactionCategory.Deposit, "cash deposit");

        Assert.Equal(AccountMovementOutcome.Success, result.Outcome);
        Assert.Equal(150m, await _database.GetBalanceAsync(accountId));

        var record = Assert.Single(await _database.GetTransactionsAsync(accountId));
        Assert.Equal(TransactionType.Credit, record.TransactionType);
        Assert.Equal(TransactionCategory.Deposit, record.TransactionCategory);
        Assert.Equal(50m, record.Amount);
    }

    [Fact]
    public async Task Withdrawal_UpdatesBalanceAndRecordsTransaction()
    {
        var accountId = await _database.CreateAccountAsync(balance: 100m);

        var result = await NewRepository().ApplyMovementAsync(
            accountId, 40m, TransactionType.Debit, TransactionCategory.Withdrawal, "atm");

        Assert.Equal(AccountMovementOutcome.Success, result.Outcome);
        Assert.Equal(60m, await _database.GetBalanceAsync(accountId));

        var record = Assert.Single(await _database.GetTransactionsAsync(accountId));
        Assert.Equal(TransactionType.Debit, record.TransactionType);
        Assert.Equal(40m, record.Amount);
    }

    private AccountRepository NewRepository()
    {
        return new AccountRepository(new SqlConnectionFactory(_database.Configuration));
    }
}
