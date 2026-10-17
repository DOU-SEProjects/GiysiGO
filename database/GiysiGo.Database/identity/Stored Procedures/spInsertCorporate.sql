CREATE PROCEDURE [identity].[spInsertCorporate]
    @Id uniqueidentifier = NULL,
    @Name nvarchar(max),
    @IsActive bit
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF DATALENGTH(@Name) > 400 THROW 51000, 'Name exceeds the maximum length.', 1;
    IF NULLIF(LTRIM(RTRIM(@Name)), '') IS NULL THROW 51000, 'Name is required.', 1;
    SET @Id = COALESCE(NULLIF(@Id, '00000000-0000-0000-0000-000000000000'), NEWID());
    INSERT [identity].[corporates] (id, name, is_active)
    VALUES (@Id, @Name, @IsActive);
    SELECT @Id AS Id;
END;
GO
