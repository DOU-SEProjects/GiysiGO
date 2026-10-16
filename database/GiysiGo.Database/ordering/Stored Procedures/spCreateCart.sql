CREATE PROCEDURE [ordering].[spCreateCart]
    @Id uniqueidentifier, @CustomerId uniqueidentifier, @StoreId uniqueidentifier, @AddressId uniqueidentifier,
    @PickupWindowStart datetime2(7) = NULL, @PickupWindowEnd datetime2(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @Id IS NULL OR @Id = '00000000-0000-0000-0000-000000000000' SET @Id = NEWID();
    IF NOT EXISTS (SELECT 1 FROM [identity].customer_addresses WHERE id = @AddressId AND user_id = @CustomerId AND is_active = 1 AND delete_date IS NULL)
        THROW 51010, 'Address is not active or not owned by customer.', 1;
    IF NOT EXISTS (SELECT 1 FROM [catalog].stores WHERE id = @StoreId AND is_active = 1 AND delete_date IS NULL)
        THROW 51016, 'Store is not active.', 1;
    INSERT [ordering].carts (id, customer_id, store_id, address_id, pickup_window_start, pickup_window_end)
    VALUES (@Id, @CustomerId, @StoreId, @AddressId, @PickupWindowStart, @PickupWindowEnd);
    SELECT @Id AS Id;
END;
GO
