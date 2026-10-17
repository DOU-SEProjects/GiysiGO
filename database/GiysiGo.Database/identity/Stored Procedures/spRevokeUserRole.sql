CREATE PROCEDURE [identity].[spRevokeUserRole]
    @UserId uniqueidentifier,
    @RoleId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    UPDATE [identity].[user_roles] SET delete_date = SYSUTCDATETIME(), update_date = SYSUTCDATETIME()
    WHERE user_id = @UserId AND role_id = @RoleId AND delete_date IS NULL;
END;
GO
