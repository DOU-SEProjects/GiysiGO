CREATE PROCEDURE [ordering].[spCheckoutCart]
    @CartId uniqueidentifier,
    @CustomerId uniqueidentifier,
    @IdempotencyKey nvarchar(200),
    @ExpectedCartVersion int
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ExistingOrderId uniqueidentifier;
    SELECT @ExistingOrderId = id FROM [ordering].orders WHERE customer_id = @CustomerId AND idempotency_key = @IdempotencyKey;
    IF @ExistingOrderId IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [ordering].orders WHERE id = @ExistingOrderId AND source_cart_id = @CartId)
            THROW 51008, 'Idempotency key was already used for another cart.', 1;
        IF (SELECT source_cart_id FROM [ordering].orders WHERE id = @ExistingOrderId) <> @CartId
            THROW 51009, 'Idempotency key was already used for another cart.', 1;
        SELECT id AS OrderId, source_cart_id AS SourceCartId, store_id AS StoreId, pickup_window_start AS PickupWindowStartUtc,
               pickup_window_end AS PickupWindowEndUtc, CAST(0 AS bit) AS Created
        FROM [ordering].orders WHERE id = @ExistingOrderId;
        RETURN;
    END;

    DECLARE @StoreId uniqueidentifier, @AddressId uniqueidentifier, @PickupStart datetime2(7), @PickupEnd datetime2(7), @Version int;
    SELECT @StoreId = c.store_id, @AddressId = c.address_id, @PickupStart = c.pickup_window_start,
           @PickupEnd = c.pickup_window_end, @Version = c.version
    FROM [ordering].carts c WITH (UPDLOCK, HOLDLOCK)
    WHERE c.id = @CartId AND c.customer_id = @CustomerId AND c.status = 'OPEN' AND c.delete_date IS NULL;
    IF @StoreId IS NULL THROW 51000, 'Open cart not found for customer.', 1;
    IF @Version <> @ExpectedCartVersion THROW 51001, 'Cart version conflict.', 1;
    IF @PickupStart IS NULL OR @PickupEnd <= @PickupStart THROW 51002, 'A valid pickup window is required.', 1;
    IF NOT EXISTS (SELECT 1 FROM [identity].customer_addresses WHERE id = @AddressId AND user_id = @CustomerId AND is_active = 1 AND delete_date IS NULL)
        THROW 51003, 'Cart address is not active or not owned by customer.', 1;
    IF NOT EXISTS (SELECT 1 FROM [ordering].cart_items WHERE cart_id = @CartId AND delete_date IS NULL)
        THROW 51004, 'Cart is empty.', 1;
    IF EXISTS (SELECT 1 FROM [ordering].cart_items ci WHERE ci.cart_id = @CartId AND ci.delete_date IS NULL
               AND NOT EXISTS (SELECT 1 FROM [ordering].cart_item_services cis WHERE cis.cart_item_id = ci.id AND cis.delete_date IS NULL))
        THROW 51006, 'Every cart item must have at least one selected service.', 1;
    IF NOT EXISTS (SELECT 1 FROM [catalog].stores WHERE id = @StoreId AND is_active = 1 AND delete_date IS NULL)
        THROW 51007, 'Store is not active.', 1;
    IF NOT EXISTS (SELECT 1 FROM [identity].corporates corp JOIN [catalog].stores st ON st.corporate_id = corp.id
                   WHERE st.id = @StoreId AND corp.is_active = 1 AND corp.delete_date IS NULL)
        THROW 51017, 'Store corporate is not active.', 1;
    IF EXISTS (
        SELECT 1 FROM [ordering].cart_item_services cis
        JOIN [ordering].cart_items ci ON ci.id = cis.cart_item_id
        LEFT JOIN [catalog].store_offerings so ON so.id = cis.store_offering_id
        LEFT JOIN [catalog].product_type_services pts ON pts.id = so.product_type_service_id
        LEFT JOIN [catalog].services s ON s.id = pts.service_id
        LEFT JOIN [catalog].product_types pt ON pt.id = ci.product_type_id
        WHERE ci.cart_id = @CartId AND ci.delete_date IS NULL AND cis.delete_date IS NULL
          AND (so.id IS NULL OR so.store_id <> @StoreId OR so.is_active = 0 OR so.delete_date IS NOT NULL
               OR pts.product_type_id <> ci.product_type_id OR pts.is_active = 0 OR pts.delete_date IS NOT NULL
               OR s.is_active = 0 OR s.delete_date IS NOT NULL OR pt.is_active = 0 OR pt.delete_date IS NOT NULL)
    ) THROW 51005, 'Cart contains an unavailable or mismatched service.', 1;

    DECLARE @OrderId uniqueidentifier = NEWID();
    DECLARE @Total decimal(12,2) = (
        SELECT COALESCE(SUM(ROUND((COALESCE(so.price_override, pts.default_price) *
            CASE pts.price_unit WHEN 'M2' THEN COALESCE(ci.customer_measurement, 1) ELSE 1 END), 2)), 0)
        FROM [ordering].cart_items ci
        JOIN [ordering].cart_item_services cis ON cis.cart_item_id = ci.id AND cis.delete_date IS NULL
        JOIN [catalog].store_offerings so ON so.id = cis.store_offering_id
        JOIN [catalog].product_type_services pts ON pts.id = so.product_type_service_id
        WHERE ci.cart_id = @CartId AND ci.delete_date IS NULL
    );
    INSERT [ordering].orders (id, source_cart_id, customer_id, store_id, pickup_window_start, pickup_window_end, estimated_total, currency, idempotency_key, version)
    VALUES (@OrderId, @CartId, @CustomerId, @StoreId, @PickupStart, @PickupEnd, @Total, 'TRY', @IdempotencyKey, 0);

    INSERT [ordering].order_address_snapshots (id, order_id, address_text, latitude, longitude, contact_name, contact_phone)
    SELECT NEWID(), @OrderId, address_text, latitude, longitude, contact_name, contact_phone
    FROM [identity].customer_addresses WHERE id = @AddressId;

    DECLARE @ItemMap TABLE (CartItemId uniqueidentifier PRIMARY KEY, OrderItemId uniqueidentifier);
    MERGE [ordering].order_items AS target
    USING (SELECT ci.id, ci.product_type_id, pt.name, ci.customer_note, ci.customer_measurement
           FROM [ordering].cart_items ci JOIN [catalog].product_types pt ON pt.id = ci.product_type_id
           WHERE ci.cart_id = @CartId AND ci.delete_date IS NULL) AS source
    ON 1 = 0
    WHEN NOT MATCHED THEN INSERT (id, order_id, product_type_id, product_type_name_snapshot, customer_note, customer_measurement, tracking_code, inspection_status)
        VALUES (NEWID(), @OrderId, source.product_type_id, source.name, source.customer_note, source.customer_measurement, CONVERT(nvarchar(36), NEWID()), 'PENDING')
    OUTPUT source.id, inserted.id INTO @ItemMap (CartItemId, OrderItemId);

    INSERT [ordering].order_item_services (id, order_item_id, product_type_service_id, service_name_snapshot, estimated_unit_price, price_unit_snapshot, estimated_quantity, estimated_price, status)
    SELECT NEWID(), im.OrderItemId, pts.id, s.name, COALESCE(so.price_override, pts.default_price), pts.price_unit,
           CASE pts.price_unit WHEN 'M2' THEN COALESCE(ci.customer_measurement, 1) ELSE 1 END,
           ROUND(COALESCE(so.price_override, pts.default_price) * CASE pts.price_unit WHEN 'M2' THEN COALESCE(ci.customer_measurement, 1) ELSE 1 END, 2), 'REQUESTED'
    FROM @ItemMap im
    JOIN [ordering].cart_items ci ON ci.id = im.CartItemId
    JOIN [ordering].cart_item_services cis ON cis.cart_item_id = ci.id AND cis.delete_date IS NULL
    JOIN [catalog].store_offerings so ON so.id = cis.store_offering_id
    JOIN [catalog].product_type_services pts ON pts.id = so.product_type_service_id
    JOIN [catalog].services s ON s.id = pts.service_id;

    INSERT [ordering].order_item_photos (id, order_item_id, storage_key, photo_type)
    SELECT NEWID(), im.OrderItemId, cp.storage_key, cp.photo_type
    FROM @ItemMap im JOIN [ordering].cart_item_photos cp ON cp.cart_item_id = im.CartItemId AND cp.delete_date IS NULL;

    UPDATE [ordering].carts SET status = 'CHECKED_OUT', version = version + 1, update_date = SYSUTCDATETIME() WHERE id = @CartId;
    INSERT [ordering].order_status_history (id, order_id, from_status, to_status, source)
    VALUES (NEWID(), @OrderId, NULL, 'AWAITING_PICKUP_ASSIGNMENT', 'SYSTEM');
    SELECT @OrderId AS OrderId, @CartId AS SourceCartId, @StoreId AS StoreId, @PickupStart AS PickupWindowStartUtc,
           @PickupEnd AS PickupWindowEndUtc, CAST(1 AS bit) AS Created;
END;
GO
