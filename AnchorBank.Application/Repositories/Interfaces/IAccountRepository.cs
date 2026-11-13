using System.Data;
using AnchorBank.Application.Accounts.Models;
using AnchorBank.Domain.Entities;
using AnchorBank.Domain.Enums;

namespace AnchorBank.Application.Repositories.Interfaces;

public interface IAccountRepository
{
    Task CreateAsync(Account account);
    Task<Account?> GetByIdAsync(Guid id, Guid customerId);
    Task<IEnumerable<Account>> GetByCustomerIdAsync(Guid customerId);
    Task<long> GetNextAccountNumberAsync();

    Task<AccountMovementResult> ApplyMovementAsync(
        Guid accountId,
        Guid customerId,
        decimal amount,
        TransactionType transactionType,
        TransactionCategory transactionCategory,
        string? description);
}
