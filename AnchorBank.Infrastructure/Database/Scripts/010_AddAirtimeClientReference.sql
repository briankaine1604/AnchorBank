-- Same shape as 009: default to backfill the ALTER, then dropped so SQL cannot invent a reference.
ALTER TABLE AirtimePurchases ADD ClientReference VARCHAR(40) NOT NULL
    CONSTRAINT DF_AirtimePurchases_ClientReference DEFAULT NEWID();
ALTER TABLE AirtimePurchases DROP CONSTRAINT DF_AirtimePurchases_ClientReference;
CREATE UNIQUE INDEX UX_AirtimePurchases_Account_ClientReference
    ON AirtimePurchases (AccountId, ClientReference);