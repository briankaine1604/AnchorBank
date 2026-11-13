using AnchorBank.Application.Accounts.DTOs;
using AnchorBank.Application.Accounts.Models;
using AnchorBank.Application.Transactions.Models;
using AnchorBank.Domain.Entities;

namespace AnchorBank.Application.Services.Interfaces;

public interface IAccountService
{
    Task<Account?> CreateAsync(CreateAccountRequest request);
    Task<Account?> GetByIdAsync(Guid id);
    Task<IEnumerable<Account>> GetByCustomerIdAsync(Guid customerId);
    Task<AccountMovementResult> DepositAsync(Guid accountId, AccountMovementRequest request);
    Task<AccountMovementResult> WithdrawAsync(Guid accountId, AccountMovementRequest request);
    Task<TransactionPage?> GetTransactionsAsync(Guid accountId, int page, int pageSize);
    Task<AccountBalanceResponse?> GetBalanceAsync(Guid accountId);
}
