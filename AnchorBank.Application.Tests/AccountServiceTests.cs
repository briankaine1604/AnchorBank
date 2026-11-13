using AnchorBank.Application.Accounts.DTOs;
using AnchorBank.Application.Accounts.Models;
using AnchorBank.Application.Customers.DTOs;
using AnchorBank.Application.Repositories.Interfaces;
using AnchorBank.Application.Services;
using AnchorBank.Application.Transactions.Models;
using AnchorBank.Domain.Entities;
using AnchorBank.Domain.Enums;

namespace AnchorBank.Application.Tests;

public class AccountServiceTests
{
    [Fact]
    public async Task Deposit_RecordsCreditWithDepositCategory()
    {
        var repository = new FakeAccountRepository { Result = new(AccountMovementOutcome.Success, 150m) };
        var service = NewService(repository);

        var result = await service.DepositAsync(Guid.NewGuid(), Guid.NewGuid(), new AccountMovementRequest(50m, "cash"));

        Assert.Equal(AccountMovementOutcome.Success, result.Outcome);
        Assert.Equal(150m, result.Balance);
        Assert.Equal(TransactionType.Credit, repository.TransactionType);
        Assert.Equal(TransactionCategory.Deposit, repository.TransactionCategory);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Deposit_WithNonPositiveAmount_ReturnsInvalidAmount(decimal amount)
    {
        var repository = new FakeAccountRepository();
        var service = NewService(repository);

        var result = await service.DepositAsync(Guid.NewGuid(), Guid.NewGuid(), new AccountMovementRequest(amount, null));

        Assert.Equal(AccountMovementOutcome.InvalidAmount, result.Outcome);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task GetBalance_ReturnsPersistedBalance()
    {
        var account = NewAccount(balance: 25000.00m);
        var service = NewService(new FakeAccountRepository { AccountToReturn = account });

        var result = await service.GetBalanceAsync(account.Id, account.CustomerId);

        Assert.NotNull(result);
        Assert.Equal(25000.00m, result.Balance);
        Assert.Equal("NGN", result.Currency);
    }

    private static Account NewAccount(decimal balance)
    {
        return new Account(
            Guid.NewGuid(),
            "0000000001",
            balance,
            AccountType.Current,
            AccountStatus.Active,
            Guid.NewGuid(),
            DateTime.UtcNow);
    }

    private static AccountService NewService(IAccountRepository repository)
    {
        return new AccountService(repository, new FakeCustomerRepository(), new FakeTransactionRepository());
    }

    private sealed class FakeAccountRepository : IAccountRepository
    {
        public AccountMovementResult Result { get; set; } = new(AccountMovementOutcome.Success);
        public Account? AccountToReturn { get; set; }
        public int CallCount { get; private set; }
        public TransactionType TransactionType { get; private set; }
        public TransactionCategory TransactionCategory { get; private set; }

        public Task<AccountMovementResult> ApplyMovementAsync(
            Guid accountId,
            Guid customerId,
            decimal amount,
            TransactionType transactionType,
            TransactionCategory transactionCategory,
            string? description)
        {
            CallCount++;
            TransactionType = transactionType;
            TransactionCategory = transactionCategory;
            return Task.FromResult(Result);
        }

        public Task<Account?> GetByIdAsync(Guid id, Guid customerId) => Task.FromResult(AccountToReturn);

        public Task CreateAsync(Account account) => throw new NotSupportedException();

        public Task<IEnumerable<Account>> GetByCustomerIdAsync(Guid customerId) => throw new NotSupportedException();

        public Task<long> GetNextAccountNumberAsync() => throw new NotSupportedException();
    }

    private sealed class FakeTransactionRepository : ITransactionRepository
    {
        public Task<TransactionPage?> GetPageByAccountIdAsync(Guid accountId, Guid customerId, int page, int pageSize)
            => throw new NotSupportedException();
    }

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        public Task<bool> TryCreateAsync(Customer customer) => throw new NotSupportedException();

        public Task<Customer?> GetByIdAsync(Guid id) => throw new NotSupportedException();

        public Task<Customer?> GetByEmailAsync(string email) => throw new NotSupportedException();
    }
}
