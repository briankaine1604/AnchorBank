using AnchorBank.Domain.Enums;


namespace AnchorBank.Domain.Entities
{
    public class Account
    {
        public Guid Id { get; private set; }

        public string AccountNumber { get; private set; }

        public Guid CustomerId { get; private set; }

        public decimal Balance { get; private set; }

        public AccountType AccountType { get; private set; }

        public AccountStatus AccountStatus { get; private set; }

        public DateTime CreatedAt { get; private set; }


        public Account(string accountNumber, Guid customerId, AccountType accountType)
            : this(Guid.NewGuid(), accountNumber, 0.00m, accountType, AccountStatus.Active, customerId, DateTime.UtcNow)
        {
        }

        public Account(Guid id, string accountNumber, decimal balance, AccountType accountType, AccountStatus accountStatus, Guid customerId, DateTime createdAt)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Account ID must be provided", nameof(id));
            }
            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                throw new ArgumentException("Account number must be provided", nameof(accountNumber));
            }
            if (customerId == Guid.Empty)
            {
                throw new ArgumentException("Customer ID must be provided", nameof(customerId));
            }
            if (balance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(balance), "Balance cannot be negative");
            }
            if (!Enum.IsDefined(accountType))
            {
                throw new ArgumentException("Invalid account type", nameof(accountType));
            }
            if (!Enum.IsDefined(accountStatus))
            {
                throw new ArgumentException("Invalid account status", nameof(accountStatus));
            }

            Id = id;
            AccountNumber = accountNumber;
            CustomerId = customerId;
            Balance = balance;
            AccountType = accountType;
            AccountStatus = accountStatus;
            CreatedAt = createdAt;
        }

        public void Deposit(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(amount), "Deposit amount must be greater than zero.");

            Balance += amount;
        }

        public void Withdraw(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(amount), "Withdrawal amount must be greater than zero.");

            if (amount > Balance)
                throw new InvalidOperationException("Insufficient funds.");

            Balance -= amount;
        }
    }
}
