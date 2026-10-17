CREATE PROCEDURE [identity].[spGetAllCorporates]

AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT id AS Id, create_date AS CreateDate, update_date AS UpdateDate, delete_date AS DeleteDate, name AS Name, is_active AS IsActive FROM [identity].[corporates]
    WHERE delete_date IS NULL ORDER BY create_date, id;
END;
GO
