using AnchorBank.Application.Airtime.DTOs;
using AnchorBank.Application.Airtime.Models;

namespace AnchorBank.Application.Airtime;

public interface IAirtimeService
{
    Task<BuyAirtimeResult> BuyAsync(Guid accountId, Guid customerId, BuyAirtimeRequest request);
}