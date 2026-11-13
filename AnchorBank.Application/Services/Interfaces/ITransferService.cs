using AnchorBank.Application.Transfers.DTOs;
using AnchorBank.Application.Transfers.Models;

namespace AnchorBank.Application.Services.Interfaces;

public interface ITransferService
{
    Task<TransferResult> TransferAsync(Guid sourceAccountId, TransferRequest request);
}
