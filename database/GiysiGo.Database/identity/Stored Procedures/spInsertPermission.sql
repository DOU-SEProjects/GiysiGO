CREATE PROCEDURE [identity].[spInsertPermission]
    @Id uniqueidentifier = NULL,
    @Code nvarchar(max),
    @Description nvarchar(max),
    @IsActive bit
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF DATALENGTH(@Code) > 200 THROW 51000, 'Code exceeds the maximum length.', 1;
    IF NULLIF(LTRIM(RTRIM(@Code)), '') IS NULL THROW 51000, 'Code is required.', 1;
    IF NULLIF(LTRIM(RTRIM(@Description)), '') IS NULL THROW 51000, 'Description is required.', 1;
    SET @Id = COALESCE(NULLIF(@Id, '00000000-0000-0000-0000-000000000000'), NEWID());
    INSERT [identity].[permissions] (id, code, description, is_active)
    VALUES (@Id, @Code, @Description, @IsActive);
    SELECT @Id AS Id;
END;
GO
