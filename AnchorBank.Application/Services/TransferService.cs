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

    public Task<TransferResult> TransferAsync(Guid sourceAccountId, TransferRequest request)
    {
        if (request.Amount <= 0)
            return Task.FromResult(new TransferResult(TransferOutcome.InvalidAmount));

        return _transferRepository.TransferAsync(
            sourceAccountId,
            request.DestinationAccountNumber,
            request.Amount,
            request.Description);
    }
}
