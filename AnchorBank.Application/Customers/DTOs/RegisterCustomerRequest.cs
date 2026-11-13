namespace AnchorBank.Application.Customers.DTOs;

public record RegisterCustomerRequest(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Password
);
