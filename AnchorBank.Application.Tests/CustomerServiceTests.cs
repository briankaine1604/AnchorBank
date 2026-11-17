using System.Text.Json;
using AnchorBank.Application.Customers.DTOs;
using AnchorBank.Application.Customers.Models;
using AnchorBank.Application.Repositories.Interfaces;
using AnchorBank.Application.Security;
using AnchorBank.Application.Services;
using AnchorBank.Domain.Entities;

namespace AnchorBank.Application.Tests;

public class CustomerServiceTests
{
    [Fact]
    public async Task CreateAsync_StoresHashInsteadOfPassword()
    {
        var repository = new FakeCustomerRepository();
        var service = new CustomerService(repository, new FakePasswordHasher());

        await service.CreateAsync(
            new RegisterCustomerRequest("Ada", "Lovelace", "ada@example.com", "08012345678", "plain-secret"));

        Assert.NotNull(repository.Inserted);
        Assert.Equal("hashed", repository.Inserted.PasswordHash);
        Assert.NotEqual("plain-secret", repository.Inserted.PasswordHash);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(129)]
    public async Task CreateAsync_RejectsPasswordOutsideLengthRange(int length)
    {
        var repository = new FakeCustomerRepository();
        var service = new CustomerService(repository, new FakePasswordHasher());

        var result = await service.CreateAsync(
            new RegisterCustomerRequest("Ada", "Lovelace", "ada@example.com", "08012345678", new string('x', length)));

        Assert.Equal(RegistrationOutcome.InvalidPassword, result.Outcome);
        Assert.Null(repository.Inserted);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(128)]
    public async Task CreateAsync_AcceptsPasswordAtLengthLimits(int length)
    {
        var repository = new FakeCustomerRepository();
        var service = new CustomerService(repository, new FakePasswordHasher());

        var result = await service.CreateAsync(
            new RegisterCustomerRequest("Ada", "Lovelace", "ada@example.com", "08012345678", new string('x', length)));

        Assert.Equal(RegistrationOutcome.Success, result.Outcome);
        Assert.NotNull(repository.Inserted);
    }

    [Fact]
    public void CustomerResponse_DoesNotExposePasswordHash()
    {
        var customer = new Customer("Ada", "Lovelace", "ada@example.com", "08012345678", "hashed-secret-value");

        var json = JsonSerializer.Serialize(CustomerResponse.From(customer));

        Assert.DoesNotContain("hashed-secret-value", json);
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => "hashed";

        public bool Verify(string password, string? storedHash) => false;
    }

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        public Customer? Inserted { get; private set; }

        public Task<bool> TryCreateAsync(Customer customer)
        {
            Inserted = customer;
            return Task.FromResult(true);
        }

        public Task<Customer?> GetByIdAsync(Guid id) => Task.FromResult(Inserted);

        public Task<Customer?> GetByEmailAsync(string email) => throw new NotSupportedException();
    }
}
