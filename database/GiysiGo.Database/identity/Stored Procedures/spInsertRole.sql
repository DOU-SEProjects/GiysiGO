CREATE PROCEDURE [identity].[spInsertRole]
    @Id uniqueidentifier = NULL,
    @Code nvarchar(max),
    @Name nvarchar(max),
    @Scope nvarchar(max),
    @IsActive bit
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF DATALENGTH(@Code) > 200 THROW 51000, 'Code exceeds the maximum length.', 1;
    IF DATALENGTH(@Name) > 400 THROW 51000, 'Name exceeds the maximum length.', 1;
    IF DATALENGTH(@Scope) > 60 THROW 51000, 'Scope exceeds the maximum length.', 1;
    IF NULLIF(LTRIM(RTRIM(@Code)), '') IS NULL THROW 51000, 'Code is required.', 1;
    IF NULLIF(LTRIM(RTRIM(@Name)), '') IS NULL THROW 51000, 'Name is required.', 1;
    IF NULLIF(LTRIM(RTRIM(@Scope)), '') IS NULL THROW 51000, 'Scope is required.', 1;
    SET @Id = COALESCE(NULLIF(@Id, '00000000-0000-0000-0000-000000000000'), NEWID());
    INSERT [identity].[roles] (id, code, name, scope, is_active)
    VALUES (@Id, @Code, @Name, @Scope, @IsActive);
    SELECT @Id AS Id;
END;
GO
