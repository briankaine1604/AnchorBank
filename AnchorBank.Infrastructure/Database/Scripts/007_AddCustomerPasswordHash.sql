-- Customers registered before password support have PasswordHash = NULL; those rows are left untouched.
-- The constraint rejects anything that is not a hash produced by Pbkdf2PasswordHasher.
ALTER TABLE Customers
ADD PasswordHash NVARCHAR(200) NULL
    CONSTRAINT CK_Customers_PasswordHash
    CHECK (PasswordHash IS NULL
           OR (LEN(PasswordHash) >= 60 AND PasswordHash LIKE 'PBKDF2-SHA256$%'));
