CREATE PROCEDURE [identity].[spDeleteUser]
    @UserId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    UPDATE [identity].users SET is_active = 0, delete_date = SYSUTCDATETIME(), update_date = SYSUTCDATETIME()
    WHERE id = @UserId AND delete_date IS NULL;
    IF @@ROWCOUNT = 0 THROW 51001, 'User was not found.', 1;
END;
GO
