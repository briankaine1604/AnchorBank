namespace AnchorBank.Application.Airtime.Models;

public enum AirtimePurchaseOutcome
{
    Success,
    Failed,
    Processing,
    Unknown
}

public record AirtimePurchaseResult(
    AirtimePurchaseOutcome Outcome,
    string? ProviderReference = null,
    string? Message = null
    );
