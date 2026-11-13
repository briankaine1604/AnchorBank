namespace AnchorBank.Application.Accounts.DTOs;

public record AccountMovementRequest(
    decimal Amount,
    string? Description
);
