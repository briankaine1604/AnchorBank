using AnchorBank.Domain.Enums;

namespace AnchorBank.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public TransactionType TransactionType { get; private set; }
    public TransactionCategory TransactionCategory { get; private set; }
    public decimal Amount { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Normal transaction creation
    public Transaction(
        Guid accountId,
        TransactionType transactionType,
        TransactionCategory transactionCategory,
        decimal amount,
        string? description)
        : this(
            Guid.NewGuid(),
            accountId,
            transactionType,
            transactionCategory,
            amount,
            description,
            DateTime.UtcNow)
    {
    }

    // Database reconstruction
    public Transaction(
        Guid id,
        Guid accountId,
        TransactionType transactionType,
        TransactionCategory transactionCategory,
        decimal amount,
        string? description,
        DateTime createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Transaction ID must be provided", nameof(id));

        if (accountId == Guid.Empty)
            throw new ArgumentException("Account ID must be provided", nameof(accountId));

        if (!Enum.IsDefined(transactionType))
            throw new ArgumentException("Invalid transaction type", nameof(transactionType));

        if (!Enum.IsDefined(transactionCategory))
            throw new ArgumentException("Invalid transaction category", nameof(transactionCategory));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero");

        if (createdAt == default)
            throw new ArgumentException("Creation date must be provided", nameof(createdAt));

        Id = id;
        AccountId = accountId;
        TransactionType = transactionType;
        TransactionCategory = transactionCategory;
        Amount = amount;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        CreatedAt = createdAt;
    }
}