CREATE PROCEDURE [identity].[spGetOwnedCustomerAddress]
    @UserId uniqueidentifier,
    @AddressId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SELECT a.id AS Id, a.create_date AS CreateDate, a.update_date AS UpdateDate, a.delete_date AS DeleteDate, a.user_id AS UserId, a.label AS Label, a.address_text AS AddressText, a.latitude AS Latitude, a.longitude AS Longitude, a.contact_name AS ContactName, a.contact_phone AS ContactPhone, a.is_active AS IsActive FROM [identity].customer_addresses a WHERE a.id = @AddressId AND a.user_id = @UserId AND a.delete_date IS NULL AND a.is_active = 1
    AND EXISTS (SELECT 1 FROM [identity].users u WHERE u.id = @UserId AND u.delete_date IS NULL AND u.is_active = 1);
END;
GO
