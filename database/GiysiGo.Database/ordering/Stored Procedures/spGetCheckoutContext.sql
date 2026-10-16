CREATE PROCEDURE [ordering].[spGetCheckoutContext]
    @CartId uniqueidentifier, @CustomerId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.id AS CartId, c.customer_id AS CustomerId, c.store_id AS StoreId, c.address_id AS AddressId,
           a.latitude AS Latitude, a.longitude AS Longitude,
           c.pickup_window_start AS PickupWindowStartUtc, c.pickup_window_end AS PickupWindowEndUtc
    FROM [ordering].carts c WITH (UPDLOCK, HOLDLOCK)
    JOIN [identity].customer_addresses a ON a.id = c.address_id AND a.user_id = c.customer_id
    WHERE c.id = @CartId AND c.customer_id = @CustomerId AND c.status = 'OPEN'
      AND c.delete_date IS NULL AND a.is_active = 1 AND a.delete_date IS NULL;
END;
GO
