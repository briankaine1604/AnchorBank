namespace AnchorBank.Application.Transfers.Models;

public enum TransferOutcome
{
    Success,
    InvalidAmount,
    SourceAccountNotFound,
    DestinationAccountNotFound,
    SameAccount,
    AccountInactive,
    InsufficientFunds,
    InvalidClientReference,
    ClientReferenceReused
}

public record TransferResult(
    TransferOutcome Outcome,
    Guid TransferId = default,
    decimal SourceBalance = 0,
    decimal DestinationBalance = 0
);
