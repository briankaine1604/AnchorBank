using AnchorBank.Domain.Enums;

namespace AnchorBank.Application.Transactions.DTOs;

public record CreateTransactionRequest(
    Guid AccountId,
    decimal Amount,
    TransactionCategory Category,
    string? Description
);
