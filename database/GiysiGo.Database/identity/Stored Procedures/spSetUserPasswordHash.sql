CREATE PROCEDURE [identity].[spSetUserPasswordHash]
    @UserId uniqueidentifier,
    @PasswordHash nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NULLIF(LTRIM(RTRIM(@PasswordHash)), '') IS NULL THROW 51000, 'Password hash is required.', 1;
    UPDATE [identity].users SET password_hash = @PasswordHash, update_date = SYSUTCDATETIME()
    WHERE id = @UserId AND delete_date IS NULL;
    IF @@ROWCOUNT = 0 THROW 51001, 'User was not found.', 1;
END;
GO
