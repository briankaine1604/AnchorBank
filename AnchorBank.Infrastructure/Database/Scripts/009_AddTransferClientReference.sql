-- The default exists only so this ALTER can backfill rows that already exist; it is dropped
-- straight away, because a default here lets SQL invent a reference instead of storing one.
ALTER TABLE Transfers ADD ClientReference VARCHAR(40) NOT NULL
    CONSTRAINT DF_Transfers_ClientReference DEFAULT NEWID();
ALTER TABLE Transfers DROP CONSTRAINT DF_Transfers_ClientReference;
CREATE UNIQUE INDEX UX_Transfers_Source_ClientReference
    ON Transfers (SourceAccountId, ClientReference);