namespace AnchorBank.Application.Accounts.DTOs;

public record AccountBalanceResponse(
    Guid AccountId,
    string AccountNumber,
    decimal Balance,
    string Currency
);
