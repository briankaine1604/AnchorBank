using System.Data;
using AnchorBank.Domain.Entities;

namespace AnchorBank.Application.Repositories.Interfaces;

public interface IAccountRepository
{
    Task CreateAsync(Account account);
    Task<Account?> GetByIdAsync(Guid id);
    Task<IEnumerable<Account>> GetByCustomerIdAsync(Guid customerId);
    Task<long> GetNextAccountNumberAsync();

   
}
