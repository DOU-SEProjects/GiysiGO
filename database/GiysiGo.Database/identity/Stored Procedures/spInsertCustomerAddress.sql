CREATE PROCEDURE [identity].[spInsertCustomerAddress]
    @Id uniqueidentifier = NULL,
    @UserId uniqueidentifier,
    @Label nvarchar(max),
    @AddressText nvarchar(max),
    @Latitude decimal(9,6),
    @Longitude decimal(9,6),
    @ContactName nvarchar(max),
    @ContactPhone nvarchar(max),
    @IsActive bit
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF DATALENGTH(@Label) > 200 THROW 51000, 'Label exceeds the maximum length.', 1;
    IF DATALENGTH(@ContactName) > 400 THROW 51000, 'ContactName exceeds the maximum length.', 1;
    IF DATALENGTH(@ContactPhone) > 60 THROW 51000, 'ContactPhone exceeds the maximum length.', 1;
    IF NOT EXISTS (SELECT 1 FROM [identity].users WITH (UPDLOCK, HOLDLOCK) WHERE id = @UserId AND delete_date IS NULL AND is_active = 1)
        THROW 51001, 'Active user was not found.', 1;
    IF NULLIF(LTRIM(RTRIM(@Label)), '') IS NULL THROW 51000, 'Label is required.', 1;
    IF NULLIF(LTRIM(RTRIM(@AddressText)), '') IS NULL THROW 51000, 'AddressText is required.', 1;
    IF NULLIF(LTRIM(RTRIM(@ContactName)), '') IS NULL THROW 51000, 'ContactName is required.', 1;
    IF NULLIF(LTRIM(RTRIM(@ContactPhone)), '') IS NULL THROW 51000, 'ContactPhone is required.', 1;
    SET @Id = COALESCE(NULLIF(@Id, '00000000-0000-0000-0000-000000000000'), NEWID());
    INSERT [identity].customer_addresses (id, user_id, label, address_text, latitude, longitude, contact_name, contact_phone, is_active)
    VALUES (@Id, @UserId, @Label, @AddressText, @Latitude, @Longitude, @ContactName, @ContactPhone, @IsActive);
    SELECT @Id AS Id;
END;
GO
