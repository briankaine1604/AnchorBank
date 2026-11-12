using AnchorBank.Application.Customers.DTOs;
using AnchorBank.Application.Repositories.Interfaces;
using AnchorBank.Application.Services.Interfaces;
using AnchorBank.Domain.Entities;

namespace AnchorBank.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Customer> CreateAsync(CreateCustomerRequest request)
    {
        var customer = new Customer(
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber);

        await _customerRepository.CreateAsync(customer);
        return customer;
    }

    public Task<Customer?> GetByIdAsync(Guid id)
    {
        return _customerRepository.GetByIdAsync(id);
    }
}
