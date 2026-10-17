CREATE PROCEDURE [identity].[spUpdateCorporate]
    @Id uniqueidentifier,
    @Name nvarchar(max),
    @IsActive bit
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF DATALENGTH(@Name) > 400 THROW 51000, 'Name exceeds the maximum length.', 1;
    IF NULLIF(LTRIM(RTRIM(@Name)), '') IS NULL THROW 51000, 'Name is required.', 1;
    UPDATE [identity].[corporates] SET name = @Name, is_active = @IsActive, update_date = SYSUTCDATETIME()
    WHERE id = @Id AND delete_date IS NULL;
    IF @@ROWCOUNT = 0 THROW 51001, 'Corporate was not found.', 1;
END;
GO
