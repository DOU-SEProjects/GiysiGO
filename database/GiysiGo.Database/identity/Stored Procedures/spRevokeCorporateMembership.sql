CREATE PROCEDURE [identity].[spRevokeCorporateMembership]
    @UserId uniqueidentifier,
    @CorporateId uniqueidentifier,
    @RoleId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    UPDATE [identity].[corporate_memberships] SET delete_date = SYSUTCDATETIME(), update_date = SYSUTCDATETIME(), is_active = 0
    WHERE user_id = @UserId AND corporate_id = @CorporateId AND role_id = @RoleId AND delete_date IS NULL;
END;
GO
