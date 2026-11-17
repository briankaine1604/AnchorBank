using AnchorBank.Application.Customers.DTOs;

namespace AnchorBank.Application.Customers.Models;

public enum RegistrationOutcome
{
    Success,
    InvalidField,
    InvalidEmail,
    InvalidPassword,
    EmailAlreadyRegistered
}

public record RegistrationResult(
    RegistrationOutcome Outcome,
    CustomerResponse? Customer = null
);
