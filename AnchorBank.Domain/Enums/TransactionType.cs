using System;
using System.Collections.Generic;
using System.Text;

namespace AnchorBank.Domain.Enums
{
    public enum TransactionType
    {
        Credit = 1,
        Debit = 2
    }

    public enum TransactionCategory
    {
        Deposit = 1,
        Withdrawal = 2,
        Transfer = 3,
        AirtimePurchase = 4,
        DataPurchase = 5
    }
}
