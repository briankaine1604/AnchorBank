r-- NULL on purpose: only the row of the account that supplied the key carries it, so the
-- destination credit row has nothing to store. No backfill default, because NULL is the
-- correct value for every row that already exists.
ALTER TABLE Transactions ADD ClientReference VARCHAR(40) NULL;
