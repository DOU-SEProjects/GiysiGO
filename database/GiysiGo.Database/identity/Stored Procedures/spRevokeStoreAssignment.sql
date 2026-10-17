CREATE PROCEDURE [identity].[spRevokeStoreAssignment]
    @UserId uniqueidentifier,
    @StoreId uniqueidentifier,
    @RoleId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    UPDATE [identity].[store_assignments] SET delete_date = SYSUTCDATETIME(), update_date = SYSUTCDATETIME(), is_active = 0
    WHERE user_id = @UserId AND store_id = @StoreId AND role_id = @RoleId AND delete_date IS NULL;
END;
GO
