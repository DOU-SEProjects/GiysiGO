CREATE PROCEDURE [identity].[spDeleteOwnedCustomerAddress]
    @UserId uniqueidentifier,
    @AddressId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NOT EXISTS (SELECT 1 FROM [identity].users WITH (UPDLOCK, HOLDLOCK) WHERE id = @UserId AND delete_date IS NULL AND is_active = 1)
        THROW 51001, 'Active user was not found.', 1;
    UPDATE [identity].customer_addresses SET delete_date = SYSUTCDATETIME(), update_date = SYSUTCDATETIME(), is_active = 0
    WHERE id = @AddressId AND user_id = @UserId AND delete_date IS NULL;
    IF @@ROWCOUNT = 0 THROW 51001, 'Address was not found for the user.', 1;
END;
GO
