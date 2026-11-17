using AnchorBank.Application.Airtime.DTOs;
using AnchorBank.Application.Airtime.Models;
using AnchorBank.Domain.Enums;
using AnchorBank.Infrastructure.Vtpass;
using System.Net;
using System.Text;

namespace AnchorBank.Infrastructure.Tests;

public class VtpassServiceTests
{
    // Captured from https://sandbox.vtpass.com/api/pay on the sandbox: 08011111111 succeeds, 08022222222 fails.
    private const string DeliveredBody = """
{
  "code": "000",
  "content": {
    "transactions": {
      "status": "delivered",
      "product_name": "MTN Airtime VTU",
      "unique_element": "08011111111",
      "unit_price": "100",
      "quantity": 1,
      "service_verification": null,
      "channel": "api",
      "commission": 3.5000000000000004,
      "total_amount": 96.5,
      "discount": null,
      "type": "Airtime Recharge",
      "email": "briankaine1604@gmail.com",
      "phone": "07068802091",
      "name": null,
      "convinience_fee": 0,
      "amount": "100",
      "platform": "api",
      "method": "api",
      "transactionId": "17949009754818174663412896",
      "commission_details": {
        "amount": 3.5000000000000004,
        "rate": "3.50",
        "rate_type": "percent",
        "computation_type": "default"
      }
    }
  },
  "response_description": "TRANSACTION SUCCESSFUL",
  "requestId": "20261117083506bd023550",
  "amount": 100,
  "transaction_date": "2026-11-17T07:36:15.000000Z",
  "purchased_code": ""
}
""";

    private const string FailedBody = """
{
  "code": "016",
  "content": {
    "transactions": {
      "status": "failed",
      "product_name": "MTN Airtime VTU",
      "unique_element": "08022222222",
      "unit_price": "100",
      "quantity": 1,
      "service_verification": null,
      "channel": "api",
      "commission": 3.5000000000000004,
      "total_amount": 96.5,
      "discount": null,
      "type": "Airtime Recharge",
      "email": "briankaine1604@gmail.com",
      "phone": "07068802091",
      "name": null,
      "convinience_fee": 0,
      "amount": "100",
      "platform": "api",
      "method": "api",
      "transactionId": "17949009811060166580304618",
      "commission_details": {
        "amount": 3.5000000000000004,
        "rate": "3.50",
        "rate_type": "percent",
        "computation_type": "default"
      }
    }
  },
  "response_description": "TRANSACTION FAILED",
  "requestId": "2026111708dc1e607f3191",
  "amount": 100,
  "transaction_date": "2026-11-17T07:36:21.000000Z",
  "purchased_code": ""
}
""";

    // 028 is "product is not whitelisted": no content.transactions, so there is no status to read.
    private const string NotWhitelistedBody = """
{
  "code": "028",
  "response_description": "PRODUCT IS NOT WHITELISTED ON YOUR ACCOUNT"
}
""";

    [Fact]
    public async Task DeliveredResponse_IsSuccess()
    {
        var result = await Buy(Service(() => Json(DeliveredBody)));

        Assert.Equal(AirtimePurchaseOutcome.Success, result.Outcome);
        Assert.Equal("17949009754818174663412896", result.ProviderReference);
    }

    [Fact]
    public async Task FailedResponse_IsFailed()
    {
        var result = await Buy(Service(() => Json(FailedBody)));

        Assert.Equal(AirtimePurchaseOutcome.Failed, result.Outcome);
        Assert.Equal("TRANSACTION FAILED", result.Message);
    }

    [Fact]
    public async Task NotWhitelisted028_IsUnknown()
    {
        var result = await Buy(Service(() => Json(NotWhitelistedBody)));

        Assert.Equal(AirtimePurchaseOutcome.Unknown, result.Outcome);
    }

    [Fact]
    public async Task UnreadableBody_IsUnknown()
    {
        var result = await Buy(Service(() => Json("<html>gateway timeout</html>")));

        Assert.Equal(AirtimePurchaseOutcome.Unknown, result.Outcome);
    }

    [Fact]
    public async Task UnreachableProvider_IsUnknown()
    {
        var result = await Buy(Service(() => throw new HttpRequestException("no network")));

        Assert.Equal(AirtimePurchaseOutcome.Unknown, result.Outcome);
        Assert.Equal("Could not reach the provider", result.Message);
    }

    [Fact]
    public async Task Timeout_IsUnknown()
    {
        var result = await Buy(Service(() => throw new TaskCanceledException()));

        Assert.Equal(AirtimePurchaseOutcome.Unknown, result.Outcome);
        Assert.Equal("The provider did not respond in time.", result.Message);
    }

    [Fact]
    public async Task RequeryDelivered_IsSuccess()
    {
        var handler = new StubHandler(() => Json(DeliveredBody));
        var service = new VtpassService(new HttpClient(handler) { BaseAddress = BaseAddress });

        var result = await service.RequeryAsync("20261117083000abcdef1234");

        Assert.Equal(AirtimePurchaseOutcome.Success, result.Outcome);
        Assert.Equal("/api/requery", handler.LastRequestUri?.AbsolutePath);
    }

    private static readonly Uri BaseAddress = new("https://sandbox.vtpass.com/api/");

    private static VtpassService Service(Func<HttpResponseMessage> respond)
        => new(new HttpClient(new StubHandler(respond)) { BaseAddress = BaseAddress });

    private static Task<AirtimePurchaseResult> Buy(VtpassService service)
        => service.PurchaseAsync(
            new AirtimePurchaseRequest("20261117083000abcdef1234", Network.Mtn, "08011111111", 100));

    private static HttpResponseMessage Json(string body)
        => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _respond;

        public StubHandler(Func<HttpResponseMessage> respond) => _respond = respond;

        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(_respond());
        }
    }
}
