using AnchorBank.Application.Accounts.DTOs;
using AnchorBank.Domain.Entities;

namespace AnchorBank.Application.Services.Interfaces;

public interface IAccountService
{
    Task<Account?> CreateAsync(CreateAccountRequest request);
    Task<Account?> GetByIdAsync(Guid id);
    Task<IEnumerable<Account>> GetByCustomerIdAsync(Guid customerId);
}
