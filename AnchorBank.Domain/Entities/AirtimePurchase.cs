using AnchorBank.Domain.Enums;

namespace AnchorBank.Domain.Entities
{
    public class AirtimePurchase
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid AccountId { get; private set; }

        public Network Network { get; private set; }

        public string PhoneNumber { get; private set; }

        public decimal Amount { get; private set; }

        public string RequestId { get; private set; }

        public AirtimePurchaseStatus Status { get; private set; } = AirtimePurchaseStatus.Pending;

        public string? ProviderReference { get; private set; }

        public string? ProviderMessage { get; private set; }

        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; private set; }

        public AirtimePurchase(Guid accountId, Network network, string phoneNumber, decimal amount, string requestId)
        {
            if (accountId == Guid.Empty)
            {
                throw new ArgumentException("Account ID must be provided", nameof(accountId));
            }
            if (!Enum.IsDefined(network))
            {
                throw new ArgumentException("Invalid network", nameof(network));
            }
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                throw new ArgumentException("Phone number must be provided", nameof(phoneNumber));
            }
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero");
            }
            if (string.IsNullOrWhiteSpace(requestId))
            {
                throw new ArgumentException("Request ID must be provided", nameof(requestId));
            }

            AccountId = accountId;
            Network = network;
            PhoneNumber = phoneNumber.Trim();
            Amount = amount;
            RequestId = requestId.Trim();
        }

        // Rehydration for the repository; the table constraints already guarantee the rest.
        public AirtimePurchase(
            Guid id,
            Guid accountId,
            Network network,
            string phoneNumber,
            decimal amount,
            string requestId,
            AirtimePurchaseStatus status,
            string? providerReference,
            string? providerMessage,
            DateTime createdAt,
            DateTime? updatedAt)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Airtime purchase ID must be provided", nameof(id));
            }
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentException("Invalid airtime status", nameof(status));
            }

            Id = id;
            AccountId = accountId;
            Network = network;
            PhoneNumber = phoneNumber;
            Amount = amount;
            RequestId = requestId;
            Status = status;
            ProviderReference = providerReference;
            ProviderMessage = providerMessage;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public void Settle(AirtimePurchaseStatus status, string? providerReference, string? providerMessage)
        {
            if (!CanMove(Status, status))
                throw new InvalidOperationException($"Cannot move an airtime purchase from {Status} to {status}.");
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentException("Invalid airtime status", nameof(status));
            }

            Status = status;
            ProviderReference = providerReference;
            ProviderMessage = providerMessage;
            UpdatedAt = DateTime.UtcNow;
        }

        private static bool CanMove(AirtimePurchaseStatus from, AirtimePurchaseStatus to) => from switch
        {
            AirtimePurchaseStatus.Pending or AirtimePurchaseStatus.Processing or AirtimePurchaseStatus.Unknown
                => to is AirtimePurchaseStatus.Processing or AirtimePurchaseStatus.Unknown
                      or AirtimePurchaseStatus.Success or AirtimePurchaseStatus.Failed,
            AirtimePurchaseStatus.Failed => to is AirtimePurchaseStatus.Refunded,
            _ => false
        };
    }
}
