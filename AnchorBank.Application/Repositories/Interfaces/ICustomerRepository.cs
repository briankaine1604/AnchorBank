using AnchorBank.Domain.Entities;

namespace AnchorBank.Application.Repositories.Interfaces;

public interface ICustomerRepository
{
    Task CreateAsync(Customer customer);

    Task<Customer?> GetByIdAsync(Guid id);
}