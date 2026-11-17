using System.Text.RegularExpressions;
using AnchorBank.Application.Airtime.DTOs;
using AnchorBank.Application.Airtime.Models;
using AnchorBank.Domain.Enums;

namespace AnchorBank.Application.Airtime;

public class AirtimeService : IAirtimeService
{
    private static readonly Regex PhoneFormat = new(@"^0\d{10}$");

    private readonly IAirtimePurchaseRepository _repository;
    private readonly IAirtimeProvider _provider;

    public AirtimeService(IAirtimePurchaseRepository repository, IAirtimeProvider provider)
    {
        _repository = repository;
        _provider = provider;
    }

    public async Task<BuyAirtimeResult> BuyAsync(Guid accountId, Guid customerId, BuyAirtimeRequest request)
    {
        if (request.Amount <= 0)
            return new BuyAirtimeResult(BuyAirtimeOutcome.InvalidAmount);
        if (!Enum.IsDefined(request.Network))
            return new BuyAirtimeResult(BuyAirtimeOutcome.InvalidNetwork);
        if (!PhoneFormat.IsMatch(request.PhoneNumber ?? string.Empty))
            return new BuyAirtimeResult(BuyAirtimeOutcome.InvalidPhoneNumber);

        var requestId = _provider.GenerateRequestId();

        // 1. Debit + record the purchase as Pending.
        var begin = await _repository.BeginAsync(
            accountId, customerId, request.Network, request.PhoneNumber, request.Amount, requestId);

        if (begin.Outcome != BuyAirtimeOutcome.Success)
            return new BuyAirtimeResult(begin.Outcome);

        var purchase = begin.Purchase!;
        var balance = begin.Balance;

        // 2. Call VTpass. CancellationToken.None on purpose: if the client disconnects,
        // we must not abandon a payment that is already in flight.
        var result = await _provider.PurchaseAsync(
            new AirtimePurchaseRequest(requestId, request.Network, request.PhoneNumber, request.Amount),
            CancellationToken.None);

        var status = result.Outcome switch
        {
            AirtimePurchaseOutcome.Success => AirtimePurchaseStatus.Success,
            AirtimePurchaseOutcome.Failed => AirtimePurchaseStatus.Failed,
            AirtimePurchaseOutcome.Processing => AirtimePurchaseStatus.Processing,
            _ => AirtimePurchaseStatus.Unknown
        };

        purchase.Settle(status, result.ProviderReference, result.Message);
        await _repository.SettleAsync(purchase);

        // 3. Refund only on an explicit Failed. Processing/Unknown stay as they are.
        if (status == AirtimePurchaseStatus.Failed)
        {
            purchase.Settle(AirtimePurchaseStatus.Refunded, null, purchase.ProviderMessage);
            var refundedBalance = await _repository.RefundAsync(purchase);
            if (refundedBalance is not null)
                balance = refundedBalance.Value;
        }

        return new BuyAirtimeResult(BuyAirtimeOutcome.Success, purchase.Id, purchase.Status, balance);
    }
}