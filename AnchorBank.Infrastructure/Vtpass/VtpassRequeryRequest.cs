using System.Text.Json.Serialization;

namespace AnchorBank.Infrastructure.Vtpass;

internal record VtpassRequeryRequest(
    [property: JsonPropertyName("request_id")] string RequestId);
