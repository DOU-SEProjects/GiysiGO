CREATE PROCEDURE [identity].[spUpdateRole]
    @Id uniqueidentifier,
    @Name nvarchar(max),
    @IsActive bit
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF DATALENGTH(@Name) > 400 THROW 51000, 'Name exceeds the maximum length.', 1;
    IF NULLIF(LTRIM(RTRIM(@Name)), '') IS NULL THROW 51000, 'Name is required.', 1;
    UPDATE [identity].[roles] SET name = @Name, is_active = @IsActive, update_date = SYSUTCDATETIME()
    WHERE id = @Id AND delete_date IS NULL;
    IF @@ROWCOUNT = 0 THROW 51001, 'Role was not found.', 1;
END;
GO
