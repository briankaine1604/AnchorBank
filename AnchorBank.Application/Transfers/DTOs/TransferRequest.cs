namespace AnchorBank.Application.Transfers.DTOs;

public record TransferRequest(
    string DestinationAccountNumber,
    decimal Amount,
    string? Description
);
