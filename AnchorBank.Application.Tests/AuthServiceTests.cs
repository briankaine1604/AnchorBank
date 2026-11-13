using AnchorBank.Application.Auth.DTOs;
using AnchorBank.Application.Auth.Models;
using AnchorBank.Application.Repositories.Interfaces;
using AnchorBank.Application.Security;
using AnchorBank.Application.Services;
using AnchorBank.Domain.Entities;

namespace AnchorBank.Application.Tests;

public class AuthServiceTests
{
    private static readonly DateTime ExpiresAt = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var service = NewService(new FakeCustomerRepository { Customer = NewCustomer("hashed-secret") });

        var result = await service.LoginAsync(new LoginRequest("ada@example.com", "secret"));

        Assert.Equal(LoginOutcome.Success, result.Outcome);
        Assert.NotNull(result.Response);
        Assert.Equal("token-value", result.Response.AccessToken);
        Assert.Equal(ExpiresAt, result.Response.ExpiresAt);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsInvalidCredentials()
    {
        var service = NewService(new FakeCustomerRepository { Customer = NewCustomer("hashed-secret") });

        var result = await service.LoginAsync(new LoginRequest("ada@example.com", "wrong-password"));

        Assert.Equal(LoginOutcome.InvalidCredentials, result.Outcome);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task Login_ForUnknownEmail_ReturnsInvalidCredentials()
    {
        var service = NewService(new FakeCustomerRepository { Customer = null });

        var result = await service.LoginAsync(new LoginRequest("nobody@example.com", "secret"));

        Assert.Equal(LoginOutcome.InvalidCredentials, result.Outcome);
    }

    [Fact]
    public async Task Login_ForCustomerWithoutPasswordHash_ReturnsInvalidCredentials()
    {
        var legacy = new Customer(Guid.NewGuid(), "Legacy", "Customer", "legacy@example.com", "08012345678", null);
        var service = NewService(new FakeCustomerRepository { Customer = legacy });

        var result = await service.LoginAsync(new LoginRequest("legacy@example.com", "anything"));

        Assert.Equal(LoginOutcome.InvalidCredentials, result.Outcome);
        Assert.Null(result.Response);
    }

    private static AuthService NewService(FakeCustomerRepository repository)
    {
        return new AuthService(repository, new FakePasswordHasher(), new FakeAccessTokenGenerator());
    }

    private static Customer NewCustomer(string passwordHash)
    {
        return new Customer(Guid.NewGuid(), "Ada", "Lovelace", "ada@example.com", "08012345678", passwordHash);
    }

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        public Customer? Customer { get; set; }

        public Task<Customer?> GetByEmailAsync(string email) => Task.FromResult(Customer);

        public Task<Customer?> GetByIdAsync(Guid id) => Task.FromResult(Customer);

        public Task<bool> TryCreateAsync(Customer customer) => Task.FromResult(true);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => "hashed";

        public bool Verify(string password, string? storedHash)
            => storedHash == "hashed-secret" && password == "secret";
    }

    private sealed class FakeAccessTokenGenerator : IAccessTokenGenerator
    {
        public AccessToken Generate(Customer customer) => new("token-value", ExpiresAt);
    }
}
