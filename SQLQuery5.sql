-- 1. Create a default residence row if it does not exist
IF NOT EXISTS (SELECT 1 FROM Residence WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT Residence ON;
    INSERT INTO Residence (Id, Name) VALUES (1, ' Brandon Mansion');
    SET IDENTITY_INSERT Residence OFF;
END

-- 2. Update any machines that have missing or invalid ResidenceId values
UPDATE Machines 
SET ResidenceId = 1 
WHERE ResidenceId IS NULL OR ResidenceId NOT IN (SELECT Id FROM Residence);