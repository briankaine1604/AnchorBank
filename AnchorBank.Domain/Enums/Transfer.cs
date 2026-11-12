using System;
using System.Collections.Generic;
using System.Text;

namespace AnchorBank.Domain.Enums
{
    public enum TransferType
    {
        Internal =1,
        External =2
    }

    public enum TransferStatus
    {
        Pending = 1,
        Completed = 2,
        Failed = 3
    }
}
