CREATE TABLE Transfers
(
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    SourceAccountId UNIQUEIDENTIFIER NOT NULL,
    DestinationAccountNumber VARCHAR(20) NOT NULL,
    DestinationBankCode VARCHAR(20) NULL,
    Amount DECIMAL(18, 2) NOT NULL CHECK (Amount > 0),
    TransferType INT NOT NULL,
    Status INT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    FOREIGN KEY (SourceAccountId) REFERENCES Accounts(Id)
);