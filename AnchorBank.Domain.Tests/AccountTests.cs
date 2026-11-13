using AnchorBank.Domain.Entities;
using AnchorBank.Domain.Enums;

namespace AnchorBank.Domain.Tests;

public class AccountTests
{
    [Fact]
    public void Deposit_IncreasesBalance()
    {
        var account = NewAccount(balance: 100m);

        account.Deposit(50m);

        Assert.Equal(150m, account.Balance);
    }

    [Fact]
    public void Withdraw_DecreasesBalance()
    {
        var account = NewAccount(balance: 100m);

        account.Withdraw(40m);

        Assert.Equal(60m, account.Balance);
    }

    [Fact]
    public void Withdraw_WithInsufficientFunds_Throws()
    {
        var account = NewAccount(balance: 100m);

        Assert.Throws<InvalidOperationException>(() => account.Withdraw(100.01m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Deposit_WithNonPositiveAmount_Throws(decimal amount)
    {
        var account = NewAccount(balance: 100m);

        Assert.Throws<ArgumentOutOfRangeException>(() => account.Deposit(amount));
    }

    private static Account NewAccount(decimal balance)
    {
        return new Account(
            Guid.NewGuid(),
            "0000000001",
            balance,
            AccountType.Current,
            AccountStatus.Active,
            Guid.NewGuid(),
            DateTime.UtcNow);
    }
}
