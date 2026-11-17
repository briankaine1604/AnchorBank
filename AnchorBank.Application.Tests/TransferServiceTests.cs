using AnchorBank.Application.Repositories.Interfaces;
using AnchorBank.Application.Services;
using AnchorBank.Application.Transfers.DTOs;
using AnchorBank.Application.Transfers.Models;

namespace AnchorBank.Application.Tests;

public class TransferServiceTests
{
    [Fact]
    public async Task Transfer_PassesRequestThroughToRepository()
    {
        var repository = new FakeTransferRepository();
        var service = new TransferService(repository);
        var sourceAccountId = Guid.NewGuid();

        await service.TransferAsync(
            sourceAccountId, Guid.NewGuid(), new TransferRequest("0000001002", 5000.00m, "Personal transfer"));

        Assert.Equal(sourceAccountId, repository.SourceAccountId);
        Assert.Equal("0000001002", repository.DestinationAccountNumber);
        Assert.Equal(5000.00m, repository.Amount);
    }

    private sealed class FakeTransferRepository : ITransferRepository
    {
        public TransferResult Result { get; set; } = new(TransferOutcome.Success);
        public Guid SourceAccountId { get; private set; }
        public string? DestinationAccountNumber { get; private set; }
        public decimal Amount { get; private set; }

        public Task<TransferResult> TransferAsync(
            Guid sourceAccountId,
            Guid customerId,
            string destinationAccountNumber,
            decimal amount,
            string? description)
        {
            SourceAccountId = sourceAccountId;
            DestinationAccountNumber = destinationAccountNumber;
            Amount = amount;
            return Task.FromResult(Result);
        }
    }
}
