using AnchorBank.Domain.Enums;

namespace AnchorBank.Application.Accounts.DTOs;

public record CreateAccountRequest(
    Guid CustomerId,
    AccountType AccountType
);
