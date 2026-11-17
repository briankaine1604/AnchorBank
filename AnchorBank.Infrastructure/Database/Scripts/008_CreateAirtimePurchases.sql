CREATE TABLE AirtimePurchases
(
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    AccountId UNIQUEIDENTIFIER NOT NULL,
    Network INT NOT NULL CHECK (Network IN (1, 2, 3, 4)),
    PhoneNumber VARCHAR(20) NOT NULL,
    Amount DECIMAL(18, 2) NOT NULL CHECK (Amount > 0),
    RequestId VARCHAR(40) NOT NULL UNIQUE,
    ProviderReference VARCHAR(100) NULL,
    ProviderMessage NVARCHAR(500) NULL,
    Status INT NOT NULL DEFAULT 1 CHECK (Status IN (1, 2, 3, 4, 5, 6)),
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,

    FOREIGN KEY (AccountId) REFERENCES Accounts(Id)
);
