using AnchorBank.Domain.Enums;

namespace AnchorBank.Application.Airtime.DTOs;

public record BuyAirtimeRequest(Network Network, string PhoneNumber, decimal Amount);