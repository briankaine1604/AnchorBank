using AnchorBank.Application.Airtime;
using AnchorBank.Application.Airtime.DTOs;
using AnchorBank.Application.Airtime.Models;
using AnchorBank.Domain.Enums;
using System.Net.Http.Json;
using System.Text.Json;



namespace AnchorBank.Infrastructure.Vtpass
{
    public class VtpassService : IAirtimeProvider
    {
        private readonly HttpClient _httpClient;

        public VtpassService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<AirtimePurchaseResult> PurchaseAsync(
            AirtimePurchaseRequest request,
            CancellationToken cancellationToken = default)
        {
            var payload = new VtpassPayRequest(
                request.RequestId,
                ToService(request.Network),
                request.Amount,
                request.PhoneNumber);

            return SendAsync("pay", payload, cancellationToken);
        }

        public Task<AirtimePurchaseResult> RequeryAsync(string requestId, CancellationToken cancellationToken = default)
            => SendAsync("requery", new VtpassRequeryRequest(requestId), cancellationToken);

        private async Task<AirtimePurchaseResult> SendAsync<TRequest>(
            string path,
            TRequest payload,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response;
            string body;

            try
            {
                response = await _httpClient.PostAsJsonAsync(path, payload, cancellationToken);
                body = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (HttpRequestException)
            {
                return new AirtimePurchaseResult(AirtimePurchaseOutcome.Unknown, Message: "Could not reach the provider");
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new AirtimePurchaseResult(AirtimePurchaseOutcome.Unknown, Message: "The provider did not respond in time.");
            }

            return MapResponse(body);
        }

        private static string ToService(Network network) => network switch
        {
            Network.Mtn => "mtn", Network.Glo => "glo", Network.Airtel => "airtel", Network.Etisalat => "etisalat",
            _ => throw new ArgumentOutOfRangeException(nameof(network)),
        };

        public string GenerateRequestId() => VtpassRequestId.New();

        private static AirtimePurchaseResult MapResponse(string body)
        {
            VtpassPayResponse? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<VtpassPayResponse>(body);   // ← the shape is used here
            }
            catch (JsonException)
            {
                return new AirtimePurchaseResult(AirtimePurchaseOutcome.Unknown);
            }

            var status = parsed?.Content?.Transactions?.Status?.ToLowerInvariant();

            var outcome = status switch
            {
                "delivered" => AirtimePurchaseOutcome.Success,
                "failed" or "reversed"=>AirtimePurchaseOutcome.Failed,
                "pending" or "initiated" => AirtimePurchaseOutcome.Processing,
                _ => AirtimePurchaseOutcome.Unknown
            };

            return new AirtimePurchaseResult(
                outcome,
                parsed?.Content?.Transactions?.TransactionId,
                parsed?.ResponseDescription);
        }

    }
}
