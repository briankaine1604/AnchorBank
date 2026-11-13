using AnchorBank.Application.Transfers.Models;

namespace AnchorBank.Application.Repositories.Interfaces;

public interface ITransferRepository
{
    Task<TransferResult> TransferAsync(
        Guid sourceAccountId,
        string destinationAccountNumber,
        decimal amount,
        string? description);
}
