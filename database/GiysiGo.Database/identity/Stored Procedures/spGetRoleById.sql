CREATE PROCEDURE [identity].[spGetRoleById]
    @Id uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT id AS Id, create_date AS CreateDate, update_date AS UpdateDate, delete_date AS DeleteDate, code AS Code, name AS Name, scope AS Scope, is_active AS IsActive FROM [identity].[roles]
    WHERE id = @Id AND delete_date IS NULL;
END;
GO
