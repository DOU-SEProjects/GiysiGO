CREATE PROCEDURE [identity].[spGetUserByEmail]
    @Email nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF DATALENGTH(@Email) > 640 THROW 51000, 'Email exceeds the maximum length.', 1;
    SELECT id AS Id, create_date AS CreateDate, update_date AS UpdateDate, delete_date AS DeleteDate, email AS Email, password_hash AS PasswordHash, is_active AS IsActive FROM [identity].users WHERE email = LTRIM(RTRIM(@Email)) AND delete_date IS NULL;
END;
GO
