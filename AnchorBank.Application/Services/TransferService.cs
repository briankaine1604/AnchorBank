using AnchorBank.Application.Repositories.Interfaces;
using AnchorBank.Application.Services.Interfaces;
using AnchorBank.Application.Transfers.DTOs;
using AnchorBank.Application.Transfers.Models;

namespace AnchorBank.Application.Services;

public class TransferService : ITransferService
{
    private readonly ITransferRepository _transferRepository;

    public TransferService(ITransferRepository transferRepository)
    {
        _transferRepository = transferRepository;
    }

    public Task<TransferResult> TransferAsync(
        Guid sourceAccountId,
        Guid customerId,
        TransferRequest request)
    {
        if (request.Amount <= 0)
            return Task.FromResult(new TransferResult(TransferOutcome.InvalidAmount));

        // Trimmed once here so the value stored on insert is the value a retry looks up.
        var clientReference = request.ClientReference?.Trim() ?? string.Empty;

        if (clientReference.Length == 0 || clientReference.Length > 40)
            return Task.FromResult(new TransferResult(TransferOutcome.InvalidClientReference));

        return _transferRepository.TransferAsync(
            sourceAccountId,
            customerId,
            request.DestinationAccountNumber,
            request.Amount,
            request.Description,
            clientReference);
    }
}
