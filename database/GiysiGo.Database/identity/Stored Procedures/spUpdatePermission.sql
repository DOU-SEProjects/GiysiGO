CREATE PROCEDURE [identity].[spUpdatePermission]
    @Id uniqueidentifier,
    @Description nvarchar(max),
    @IsActive bit
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NULLIF(LTRIM(RTRIM(@Description)), '') IS NULL THROW 51000, 'Description is required.', 1;
    UPDATE [identity].[permissions] SET description = @Description, is_active = @IsActive, update_date = SYSUTCDATETIME()
    WHERE id = @Id AND delete_date IS NULL;
    IF @@ROWCOUNT = 0 THROW 51001, 'Permission was not found.', 1;
END;
GO
