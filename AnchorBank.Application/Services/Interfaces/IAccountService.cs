using AnchorBank.Application.Accounts.DTOs;
using AnchorBank.Application.Accounts.Models;
using AnchorBank.Application.Transactions.Models;
using AnchorBank.Domain.Entities;

namespace AnchorBank.Application.Services.Interfaces;

public interface IAccountService
{
    Task<Account?> CreateAsync(Guid customerId, CreateAccountRequest request);
    Task<Account?> GetByIdAsync(Guid id, Guid customerId);
    Task<IEnumerable<Account>> GetByCustomerIdAsync(Guid customerId);
    Task<AccountMovementResult> DepositAsync(Guid accountId, Guid customerId, AccountMovementRequest request);
    Task<AccountMovementResult> WithdrawAsync(Guid accountId, Guid customerId, AccountMovementRequest request);
    Task<TransactionPage?> GetTransactionsAsync(Guid accountId, Guid customerId, int page, int pageSize);
    Task<AccountBalanceResponse?> GetBalanceAsync(Guid accountId, Guid customerId);
}
