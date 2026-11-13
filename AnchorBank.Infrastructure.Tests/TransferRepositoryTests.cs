using AnchorBank.Application.Transfers.Models;
using AnchorBank.Domain.Enums;
using AnchorBank.Infrastructure.Database;
using AnchorBank.Infrastructure.Repositories;

namespace AnchorBank.Infrastructure.Tests;

[Collection(DatabaseCollection.Name)]
public class TransferRepositoryTests
{
    private readonly DatabaseFixture _database;

    public TransferRepositoryTests(DatabaseFixture database)
    {
        _database = database;
    }

    [Fact]
    public async Task SuccessfulTransfer_MovesMoneyAndWritesEveryRecord()
    {
        var sourceId = await _database.CreateAccountAsync(balance: 1000m);
        var destinationId = await _database.CreateAccountAsync(balance: 500m);
        var destinationNumber = await _database.GetAccountNumberAsync(destinationId);
        var sourceCustomerId = await _database.GetCustomerIdAsync(sourceId);

        var result = await NewRepository().TransferAsync(
            sourceId, sourceCustomerId, destinationNumber, 200m, "Personal transfer");

        Assert.Equal(TransferOutcome.Success, result.Outcome);
        Assert.Equal(800m, await _database.GetBalanceAsync(sourceId));
        Assert.Equal(700m, await _database.GetBalanceAsync(destinationId));

        var debit = Assert.Single(await _database.GetTransactionsAsync(sourceId));
        Assert.Equal(TransactionType.Debit, debit.TransactionType);
        Assert.Equal(TransactionCategory.Transfer, debit.TransactionCategory);

        var credit = Assert.Single(await _database.GetTransactionsAsync(destinationId));
        Assert.Equal(TransactionType.Credit, credit.TransactionType);

        var transfer = Assert.Single(await _database.GetTransfersAsync(sourceId));
        Assert.Equal(TransferStatus.Completed, transfer.Status);
        Assert.Equal(destinationNumber, transfer.DestinationAccountNumber);
    }

    [Fact]
    public async Task InsufficientFunds_LeavesBothAccountsUntouched()
    {
        var sourceId = await _database.CreateAccountAsync(balance: 1000m);
        var destinationId = await _database.CreateAccountAsync(balance: 500m);
        var destinationNumber = await _database.GetAccountNumberAsync(destinationId);
        var sourceCustomerId = await _database.GetCustomerIdAsync(sourceId);

        var result = await NewRepository().TransferAsync(
            sourceId, sourceCustomerId, destinationNumber, 1000.01m, null);

        Assert.Equal(TransferOutcome.InsufficientFunds, result.Outcome);
        Assert.Equal(1000m, await _database.GetBalanceAsync(sourceId));
        Assert.Equal(500m, await _database.GetBalanceAsync(destinationId));
        Assert.Empty(await _database.GetTransfersAsync(sourceId));
    }

    private TransferRepository NewRepository()
    {
        return new TransferRepository(new SqlConnectionFactory(_database.Configuration));
    }
}
