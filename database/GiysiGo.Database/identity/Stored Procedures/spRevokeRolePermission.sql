CREATE PROCEDURE [identity].[spRevokeRolePermission]
    @RoleId uniqueidentifier,
    @PermissionId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    UPDATE [identity].[role_permissions] SET delete_date = SYSUTCDATETIME(), update_date = SYSUTCDATETIME()
    WHERE role_id = @RoleId AND permission_id = @PermissionId AND delete_date IS NULL;
END;
GO
