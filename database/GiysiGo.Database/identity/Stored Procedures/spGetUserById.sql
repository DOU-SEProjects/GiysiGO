CREATE PROCEDURE [identity].[spGetUserById]
    @Id uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT id AS Id, create_date AS CreateDate, update_date AS UpdateDate, delete_date AS DeleteDate, email AS Email, password_hash AS PasswordHash, is_active AS IsActive FROM [identity].users WHERE id = @Id AND delete_date IS NULL;
END;
GO
