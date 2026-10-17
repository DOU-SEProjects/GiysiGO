CREATE PROCEDURE [identity].[spGetAllPermissions]

AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT id AS Id, create_date AS CreateDate, update_date AS UpdateDate, delete_date AS DeleteDate, code AS Code, description AS Description, is_active AS IsActive FROM [identity].[permissions]
    WHERE delete_date IS NULL ORDER BY create_date, id;
END;
GO
