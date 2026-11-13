using AnchorBank.Application.Transfers.Models;

namespace AnchorBank.Application.Repositories.Interfaces;

public interface ITransferRepository
{
    Task<TransferResult> TransferAsync(
        Guid sourceAccountId,
        Guid customerId,
        string destinationAccountNumber,
        decimal amount,
        string? description);
}
