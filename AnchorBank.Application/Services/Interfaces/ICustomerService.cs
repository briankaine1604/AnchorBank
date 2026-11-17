using AnchorBank.Application.Customers.DTOs;
using AnchorBank.Application.Customers.Models;

namespace AnchorBank.Application.Services.Interfaces;

public interface ICustomerService
{
    Task<RegistrationResult> CreateAsync(RegisterCustomerRequest request);

    Task<CustomerResponse?> GetByIdAsync(Guid id);
}
