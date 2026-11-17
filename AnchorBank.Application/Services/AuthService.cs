using AnchorBank.Application.Auth.DTOs;
using AnchorBank.Application.Auth.Models;
using AnchorBank.Application.Repositories.Interfaces;
using AnchorBank.Application.Security;
using AnchorBank.Application.Services.Interfaces;

namespace AnchorBank.Application.Services;

public class AuthService : IAuthService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenGenerator _accessTokenGenerator;

    public AuthService(
        ICustomerRepository customerRepository,
        IPasswordHasher passwordHasher,
        IAccessTokenGenerator accessTokenGenerator)
    {
        _customerRepository = customerRepository;
        _passwordHasher = passwordHasher;
        _accessTokenGenerator = accessTokenGenerator;
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        var customer = await _customerRepository.GetByEmailAsync(request.Email.Trim());

        // Customers registered before passwords existed have no hash and can never log in.
        if (customer is null || customer.PasswordHash is null)
        {
            return new LoginResult(LoginOutcome.InvalidCredentials);
        }

        if (!_passwordHasher.Verify(request.Password, customer.PasswordHash))
        {
            return new LoginResult(LoginOutcome.InvalidCredentials);
        }

        var token = _accessTokenGenerator.Generate(customer);

        return new LoginResult(
            LoginOutcome.Success,
            new LoginResponse(token.Value, token.ExpiresAt));
    }
}
