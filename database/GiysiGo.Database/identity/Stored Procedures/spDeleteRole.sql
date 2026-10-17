CREATE PROCEDURE [identity].[spDeleteRole]
    @Id uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    UPDATE [identity].[roles] SET delete_date = SYSUTCDATETIME(), update_date = SYSUTCDATETIME(), is_active = 0
    WHERE id = @Id AND delete_date IS NULL;
    IF @@ROWCOUNT = 0 THROW 51001, 'Role was not found.', 1;
END;
GO
