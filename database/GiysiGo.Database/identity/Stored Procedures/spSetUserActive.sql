CREATE PROCEDURE [identity].[spSetUserActive]
    @UserId uniqueidentifier,
    @IsActive bit
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    UPDATE [identity].users SET is_active = @IsActive, update_date = SYSUTCDATETIME()
    WHERE id = @UserId AND delete_date IS NULL;
    IF @@ROWCOUNT = 0 THROW 51001, 'User was not found.', 1;
END;
GO
