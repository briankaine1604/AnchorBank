using AnchorBank.Domain.Enums;


namespace AnchorBank.Domain.Entities
{
   public class Transfer
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid SourceAccountId { get; private set; }

        public string DestinationAccountNumber { get; private set; }

        public string DestinationBankCode { get; private set; }

        public decimal Amount { get; private set; }

        public TransferType TransferType { get; private set; }

        public TransferStatus Status { get; private set; } = TransferStatus.Pending;

        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        public Transfer(Guid sourceAccountId, string destinationAccountNumber, string destinationBankCode, decimal amount, TransferType transferType)
        {
            if (sourceAccountId == Guid.Empty)
            {
                throw new ArgumentException("Source account ID must be provided", nameof(sourceAccountId));
            }
            if (!Enum.IsDefined(transferType))
            {
                throw new ArgumentException("Invalid transfer type", nameof(transferType));
            }
            if (string.IsNullOrWhiteSpace(destinationAccountNumber))
            {
                throw new ArgumentException("Destination account number must be provided", nameof(destinationAccountNumber));
            }
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero");
            }
            if (transferType == TransferType.External && string.IsNullOrWhiteSpace(destinationBankCode))
            {
                throw new ArgumentException(
                    "Destination bank code must be provided for external transfers",
                    nameof(destinationBankCode));
            }

            SourceAccountId = sourceAccountId;
            DestinationAccountNumber = destinationAccountNumber.Trim();
            DestinationBankCode = destinationBankCode?.Trim() ?? string.Empty;
            Amount = amount;
            TransferType = transferType;
        }

        public void Complete()
        {
            Status = TransferStatus.Completed;
        }
    }
}
