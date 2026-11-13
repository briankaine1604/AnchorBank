using AnchorBank.Domain.Entities;

namespace AnchorBank.Application.Transactions.Models;

public record TransactionPage(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<Transaction> Items)
{
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}
