CREATE PROCEDURE [identity].[spInsertUser]
    @Id uniqueidentifier = NULL,
    @Email nvarchar(max),
    @PasswordHash nvarchar(max),
    @IsActive bit
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF DATALENGTH(@Email) > 640 THROW 51000, 'Email exceeds the maximum length.', 1;
    SET @Email = LTRIM(RTRIM(@Email));
    IF @Email IS NULL OR LEN(@Email) <= 3 OR NULLIF(LTRIM(RTRIM(@PasswordHash)), '') IS NULL THROW 51000, 'Email and password hash are required.', 1;
    SET @Id = COALESCE(NULLIF(@Id, '00000000-0000-0000-0000-000000000000'), NEWID());
    INSERT [identity].users (id, email, password_hash, is_active) VALUES (@Id, @Email, @PasswordHash, @IsActive);
    SELECT @Id AS Id;
END;
GO
