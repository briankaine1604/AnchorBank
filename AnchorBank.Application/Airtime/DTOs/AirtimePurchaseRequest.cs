using AnchorBank.Domain.Enums;

namespace AnchorBank.Application.Airtime.DTOs;

public record AirtimePurchaseRequest(
    string RequestId,
    Network Network,
    string PhoneNumber,
    decimal Amount
);