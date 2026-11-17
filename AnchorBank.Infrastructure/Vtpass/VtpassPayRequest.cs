using System.Text.Json.Serialization;

namespace AnchorBank.Infrastructure.Vtpass;

internal record VtpassPayRequest(
    [property: JsonPropertyName("request_id")] string RequestId,
    [property: JsonPropertyName("serviceID")] string ServiceId,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("phone")] string Phone);
