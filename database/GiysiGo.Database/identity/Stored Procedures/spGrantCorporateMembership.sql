CREATE PROCEDURE [identity].[spGrantCorporateMembership]
    @UserId uniqueidentifier,
    @CorporateId uniqueidentifier,
    @RoleId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NOT EXISTS (SELECT 1 FROM [identity].roles WITH (UPDLOCK, HOLDLOCK) WHERE id = @RoleId AND delete_date IS NULL AND is_active = 1 AND scope = 'CORPORATE')
        THROW 51002, 'Active role with the required scope was not found.', 1;
    IF NOT EXISTS (SELECT 1 FROM [identity].users WITH (UPDLOCK, HOLDLOCK) WHERE id = @UserId AND delete_date IS NULL AND is_active = 1)
        THROW 51002, 'Active user was not found.', 1;
    IF NOT EXISTS (SELECT 1 FROM [identity].corporates WITH (UPDLOCK, HOLDLOCK) WHERE id = @CorporateId AND delete_date IS NULL AND is_active = 1)
        THROW 51002, 'Active corporate was not found.', 1;
    DECLARE @Id uniqueidentifier;
    SELECT @Id = id FROM [identity].[corporate_memberships] WITH (UPDLOCK, HOLDLOCK)
    WHERE user_id = @UserId AND corporate_id = @CorporateId AND role_id = @RoleId AND delete_date IS NULL;
    IF @Id IS NOT NULL
    BEGIN
        UPDATE [identity].[corporate_memberships] SET is_active = 1, update_date = SYSUTCDATETIME() WHERE id = @Id AND is_active = 0;
        SELECT @Id AS Id;
        RETURN;
    END;
    SET @Id = NEWID();
    INSERT [identity].[corporate_memberships] (id, user_id, corporate_id, role_id, role_scope)
    VALUES (@Id, @UserId, @CorporateId, @RoleId, 'CORPORATE');
    SELECT @Id AS Id;
END;
GO
