using AnchorBank.Domain.Entities;

namespace AnchorBank.Application.Customers.DTOs;

public record CustomerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber)
{
    public static CustomerResponse From(Customer customer)
    {
        return new CustomerResponse(
            customer.Id,
            customer.FirstName,
            customer.LastName,
            customer.Email,
            customer.PhoneNumber);
    }
}
