using System.Text.Json.Serialization;

namespace AnchorBank.Infrastructure.Vtpass;

internal record VtpassPayResponse(
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("response_description")] string? ResponseDescription,
    [property: JsonPropertyName("content")] VtpassContent? Content);

internal record VtpassContent(
    [property: JsonPropertyName("transactions")] VtpassTransaction? Transactions);

internal record VtpassTransaction(
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("transactionId")] string? TransactionId);