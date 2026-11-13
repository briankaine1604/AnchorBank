using System.Globalization;
using AnchorBank.Application.Accounts.DTOs;
using AnchorBank.Application.Accounts.Models;
using AnchorBank.Application.Repositories.Interfaces;
using AnchorBank.Application.Services.Interfaces;
using AnchorBank.Application.Transactions.Models;
using AnchorBank.Domain.Entities;
using AnchorBank.Domain.Enums;

namespace AnchorBank.Application.Services;

public class AccountService : IAccountService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    // Accounts store no currency; the bank is single-currency, so it is fixed here.
    private const string Currency = "NGN";

    private readonly IAccountRepository _accountRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ITransactionRepository _transactionRepository;

    public AccountService(
        IAccountRepository accountRepository,
        ICustomerRepository customerRepository,
        ITransactionRepository transactionRepository)
    {
        _accountRepository = accountRepository;
        _customerRepository = customerRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<Account?> CreateAsync(CreateAccountRequest request)
    {
       
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId);

        if (customer is null)
            return null;

        // Generate the next account number.
        var nextNumber = await _accountRepository.GetNextAccountNumberAsync();

        var accountNumber = nextNumber.ToString(
            "D10",
            CultureInfo.InvariantCulture);

        // Create the domain entity.
        var account = new Account(
            accountNumber,
            request.CustomerId,
            request.AccountType);

        // Persist it.
        await _accountRepository.CreateAsync(account);

        return account;
    }

    public Task<Account?> GetByIdAsync(Guid id)
    {
        return _accountRepository.GetByIdAsync(id);
    }

    public Task<IEnumerable<Account>> GetByCustomerIdAsync(Guid customerId)
    {
        return _accountRepository.GetByCustomerIdAsync(customerId);
    }

    public Task<AccountMovementResult> DepositAsync(Guid accountId, AccountMovementRequest request)
    {
        if (request.Amount <= 0)
            return Task.FromResult(new AccountMovementResult(AccountMovementOutcome.InvalidAmount));

        return _accountRepository.ApplyMovementAsync(
            accountId,
            request.Amount,
            TransactionType.Credit,
            TransactionCategory.Deposit,
            request.Description);
    }

    public Task<AccountMovementResult> WithdrawAsync(Guid accountId, AccountMovementRequest request)
    {
        if (request.Amount <= 0)
            return Task.FromResult(new AccountMovementResult(AccountMovementOutcome.InvalidAmount));

        return _accountRepository.ApplyMovementAsync(
            accountId,
            request.Amount,
            TransactionType.Debit,
            TransactionCategory.Withdrawal,
            request.Description);
    }

    public Task<TransactionPage?> GetTransactionsAsync(Guid accountId, int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        return _transactionRepository.GetPageByAccountIdAsync(accountId, page, pageSize);
    }

    public async Task<AccountBalanceResponse?> GetBalanceAsync(Guid accountId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);

        if (account is null)
            return null;

        return new AccountBalanceResponse(
            account.Id,
            account.AccountNumber,
            account.Balance,
            Currency);
    }
}
