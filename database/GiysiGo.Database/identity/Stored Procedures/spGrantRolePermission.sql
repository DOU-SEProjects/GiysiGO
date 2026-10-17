CREATE PROCEDURE [identity].[spGrantRolePermission]
    @RoleId uniqueidentifier,
    @PermissionId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NOT EXISTS (SELECT 1 FROM [identity].roles WITH (UPDLOCK, HOLDLOCK) WHERE id = @RoleId AND delete_date IS NULL AND is_active = 1)
        THROW 51002, 'Active role with the required scope was not found.', 1;
    IF NOT EXISTS (SELECT 1 FROM [identity].permissions WITH (UPDLOCK, HOLDLOCK) WHERE id = @PermissionId AND delete_date IS NULL AND is_active = 1)
        THROW 51002, 'Active permission was not found.', 1;
    DECLARE @Id uniqueidentifier;
    SELECT @Id = id FROM [identity].[role_permissions] WITH (UPDLOCK, HOLDLOCK)
    WHERE role_id = @RoleId AND permission_id = @PermissionId AND delete_date IS NULL;
    IF @Id IS NOT NULL
    BEGIN
        SELECT @Id AS Id;
        RETURN;
    END;
    SET @Id = NEWID();
    INSERT [identity].[role_permissions] (id, role_id, permission_id)
    VALUES (@Id, @RoleId, @PermissionId);
    SELECT @Id AS Id;
END;
GO
