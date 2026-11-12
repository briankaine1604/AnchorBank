using AnchorBank.Domain.Entities;

namespace AnchorBank.Application.Repositories.Interfaces;

public interface ITransactionRepository
{
    Task CreateAsync(Transaction transaction);
    Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid accountId);
}
