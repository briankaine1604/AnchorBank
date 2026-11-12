namespace AnchorBank.Application.Customers.DTOs;

public record CreateCustomerRequest(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber
);