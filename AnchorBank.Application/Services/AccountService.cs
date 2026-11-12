using System.Globalization;
using AnchorBank.Application.Accounts.DTOs;
using AnchorBank.Application.Repositories.Interfaces;
using AnchorBank.Application.Services.Interfaces;
using AnchorBank.Domain.Entities;

namespace AnchorBank.Application.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICustomerRepository _customerRepository;

    public AccountService(
        IAccountRepository accountRepository,
        ICustomerRepository customerRepository)
    {
        _accountRepository = accountRepository;
        _customerRepository = customerRepository;
    }

    public async Task<Account?> CreateAsync(CreateAccountRequest request)
    {
       
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId);

        if (customer is null)
            return null;

        // Generate the next account number.
        var nextNumber = await _accountRepository.GetNextAccountNumberAsync();

        var accountNumber = nextNumber.ToString(
            "D10",
            CultureInfo.InvariantCulture);

        // Create the domain entity.
        var account = new Account(
            accountNumber,
            request.CustomerId,
            request.AccountType);

        // Persist it.
        await _accountRepository.CreateAsync(account);

        return account;
    }

    public Task<Account?> GetByIdAsync(Guid id)
    {
        return _accountRepository.GetByIdAsync(id);
    }

    public Task<IEnumerable<Account>> GetByCustomerIdAsync(Guid customerId)
    {
        return _accountRepository.GetByCustomerIdAsync(customerId);
    }
}
