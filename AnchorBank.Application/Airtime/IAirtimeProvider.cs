using AnchorBank.Application.Airtime.DTOs;
using AnchorBank.Application.Airtime.Models;

namespace AnchorBank.Application.Airtime
{
    public interface IAirtimeProvider
    {
        Task<AirtimePurchaseResult> PurchaseAsync(AirtimePurchaseRequest request, CancellationToken cancellationToken = default);

        string GenerateRequestId();
        Task<AirtimePurchaseResult> RequeryAsync(string requestId, CancellationToken cancellationToken = default);

    }    
        
}
