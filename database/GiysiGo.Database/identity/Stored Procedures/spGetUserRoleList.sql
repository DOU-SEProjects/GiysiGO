CREATE PROCEDURE [identity].[spGetUserRoleList]
    @UserId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT x.id AS Id, x.create_date AS CreateDate, x.update_date AS UpdateDate, x.delete_date AS DeleteDate, x.user_id AS UserId, x.role_id AS RoleId, x.role_scope AS RoleScope FROM [identity].[user_roles] x
    WHERE x.user_id = @UserId AND x.delete_date IS NULL
    AND EXISTS (SELECT 1 FROM [identity].roles r WHERE r.id = x.role_id AND r.delete_date IS NULL AND r.is_active = 1)
    AND EXISTS (SELECT 1 FROM [identity].users u WHERE u.id = x.user_id AND u.delete_date IS NULL AND u.is_active = 1)
    ORDER BY x.create_date, x.id;
END;
GO
