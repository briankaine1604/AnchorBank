namespace AnchorBank.Application.Accounts.Models;

public enum AccountMovementOutcome
{
    Success,
    InvalidAmount,
    AccountNotFound,
    AccountInactive,
    InsufficientFunds
}

public record AccountMovementResult(
    AccountMovementOutcome Outcome,
    decimal Balance = 0
);
