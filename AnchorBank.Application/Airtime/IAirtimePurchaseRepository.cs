using AnchorBank.Application.Airtime.Models;
using AnchorBank.Domain.Entities;
using AnchorBank.Domain.Enums;

namespace AnchorBank.Application.Airtime;

public interface IAirtimePurchaseRepository
{
    // Step 1: debit the account and insert the purchase as Pending, in one SQL transaction.
    Task<BeginAirtimePurchaseResult> BeginAsync(
        Guid accountId, Guid customerId, Network network,
        string phoneNumber, decimal amount, string requestId, string clientReference);

    // Step 2: save the status the purchase was settled to.
    Task SettleAsync(AirtimePurchase purchase);

    // Step 3: credit the money back and mark the purchase Refunded. Returns the new balance,
    // or null if the purchase was not in Failed (so it can never be refunded twice).
    Task<decimal?> RefundAsync(AirtimePurchase purchase);
}