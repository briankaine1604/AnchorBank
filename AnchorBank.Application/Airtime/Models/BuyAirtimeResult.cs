using AnchorBank.Domain.Entities;
using AnchorBank.Domain.Enums;

namespace AnchorBank.Application.Airtime.Models;

public enum BuyAirtimeOutcome
{
    Success,          
    InvalidAmount,
    InvalidNetwork,
    InvalidPhoneNumber,
    AccountNotFound,
    AccountInactive,
    InsufficientFunds
}

public record BuyAirtimeResult(
    BuyAirtimeOutcome Outcome,
    Guid PurchaseId = default,
    AirtimePurchaseStatus Status = default,
    decimal Balance = 0);

public record BeginAirtimePurchaseResult(
    BuyAirtimeOutcome Outcome,
    AirtimePurchase? Purchase = null,
    decimal Balance = 0);