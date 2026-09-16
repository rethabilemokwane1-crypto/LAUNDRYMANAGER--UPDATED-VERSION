UPDATE Machines
SET IsBooked = 0, BookedBy = NULL
WHERE BookedBy NOT IN (SELECT Email FROM Users);
