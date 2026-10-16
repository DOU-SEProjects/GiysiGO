CREATE PROCEDURE [ordering].[spUpdateCartDeliveryDetails]
    @CartId uniqueidentifier, @CustomerId uniqueidentifier, @AddressId uniqueidentifier, @PickupWindowStart datetime2(7) = NULL,
    @PickupWindowEnd datetime2(7) = NULL, @ExpectedVersion int
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM [ordering].carts WITH (UPDLOCK, HOLDLOCK) WHERE id = @CartId AND customer_id = @CustomerId AND status = 'OPEN' AND version = @ExpectedVersion AND delete_date IS NULL)
        THROW 51014, 'Open cart missing or version conflict.', 1;
    IF NOT EXISTS (SELECT 1 FROM [identity].customer_addresses WHERE id = @AddressId AND user_id = @CustomerId AND is_active = 1 AND delete_date IS NULL)
        THROW 51015, 'Address is not active or not owned by customer.', 1;
    UPDATE [ordering].carts SET address_id = @AddressId, pickup_window_start = @PickupWindowStart,
        pickup_window_end = @PickupWindowEnd, version = version + 1, update_date = SYSUTCDATETIME()
    WHERE id = @CartId AND customer_id = @CustomerId AND version = @ExpectedVersion;
    IF @@ROWCOUNT <> 1 THROW 51018, 'Cart version conflict.', 1;
END;
GO
