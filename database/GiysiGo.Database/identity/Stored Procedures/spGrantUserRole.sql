CREATE PROCEDURE [identity].[spGrantUserRole]
    @UserId uniqueidentifier,
    @RoleId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NOT EXISTS (SELECT 1 FROM [identity].roles WITH (UPDLOCK, HOLDLOCK) WHERE id = @RoleId AND delete_date IS NULL AND is_active = 1 AND scope = 'GLOBAL')
        THROW 51002, 'Active role with the required scope was not found.', 1;
    IF NOT EXISTS (SELECT 1 FROM [identity].users WITH (UPDLOCK, HOLDLOCK) WHERE id = @UserId AND delete_date IS NULL AND is_active = 1)
        THROW 51002, 'Active user was not found.', 1;
    DECLARE @Id uniqueidentifier;
    SELECT @Id = id FROM [identity].[user_roles] WITH (UPDLOCK, HOLDLOCK)
    WHERE user_id = @UserId AND role_id = @RoleId AND delete_date IS NULL;
    IF @Id IS NOT NULL
    BEGIN
        SELECT @Id AS Id;
        RETURN;
    END;
    SET @Id = NEWID();
    INSERT [identity].[user_roles] (id, user_id, role_id, role_scope)
    VALUES (@Id, @UserId, @RoleId, 'GLOBAL');
    SELECT @Id AS Id;
END;
GO
