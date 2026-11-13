using AnchorBank.Application.Transactions.Models;

namespace AnchorBank.Application.Repositories.Interfaces;

public interface ITransactionRepository
{
    Task<TransactionPage?> GetPageByAccountIdAsync(Guid accountId, int page, int pageSize);
}
