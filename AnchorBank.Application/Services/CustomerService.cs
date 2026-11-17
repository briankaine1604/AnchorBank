using System.Net.Mail;
using AnchorBank.Application.Customers.DTOs;
using AnchorBank.Application.Customers.Models;
using AnchorBank.Application.Repositories.Interfaces;
using AnchorBank.Application.Security;
using AnchorBank.Application.Services.Interfaces;
using AnchorBank.Domain.Entities;

namespace AnchorBank.Application.Services;

public class CustomerService : ICustomerService
{
    private const int MinPasswordLength = 8;
    private const int MaxPasswordLength = 128;
    private const int MaxFirstNameLength = 100;
    private const int MaxLastNameLength = 100;
    private const int MaxEmailLength = 255;
    private const int MaxPhoneNumberLength = 20;

    private readonly ICustomerRepository _customerRepository;
    private readonly IPasswordHasher _passwordHasher;

    public CustomerService(
        ICustomerRepository customerRepository,
        IPasswordHasher passwordHasher)
    {
        _customerRepository = customerRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegistrationResult> CreateAsync(RegisterCustomerRequest request)
    {
        if (request.FirstName is null || request.FirstName.Trim().Length > MaxFirstNameLength
            || request.LastName is null || request.LastName.Trim().Length > MaxLastNameLength
            || request.Email is null || request.Email.Trim().Length > MaxEmailLength
            || request.PhoneNumber is null || request.PhoneNumber.Trim().Length > MaxPhoneNumberLength)
        {
            return new RegistrationResult(RegistrationOutcome.InvalidField);
        }

        if (string.IsNullOrEmpty(request.Password)
            || request.Password.Length < MinPasswordLength
            || request.Password.Length > MaxPasswordLength)
        {
            return new RegistrationResult(RegistrationOutcome.InvalidPassword);
        }

        var email = request.Email.Trim();

        // Email is the login identifier, so only a bare, parseable address is accepted.
        if (!MailAddress.TryCreate(email, out var address)
            || address is null
            || !string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase))
        {
            return new RegistrationResult(RegistrationOutcome.InvalidEmail);
        }

        var customer = new Customer(
            request.FirstName,
            request.LastName,
            email,
            request.PhoneNumber,
            _passwordHasher.Hash(request.Password));

        if (!await _customerRepository.TryCreateAsync(customer))
            return new RegistrationResult(RegistrationOutcome.EmailAlreadyRegistered);

        return new RegistrationResult(RegistrationOutcome.Success, CustomerResponse.From(customer));
    }

    public async Task<CustomerResponse?> GetByIdAsync(Guid id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);

        return customer is null ? null : CustomerResponse.From(customer);
    }
}
