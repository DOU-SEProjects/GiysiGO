CREATE PROCEDURE [ordering].[spSetDraftQuoteLine]
    @QuoteId uniqueidentifier, @StoreId uniqueidentifier, @OrderItemServiceId uniqueidentifier,
    @UnitPrice decimal(12,2), @PriceUnit nvarchar(20), @Quantity decimal(12,3), @Explanation nvarchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @UnitPrice < 0 OR @Quantity <= 0 OR @PriceUnit NOT IN ('PIECE','M2')
        THROW 51050, 'Quote line values are invalid.', 1;
    DECLARE @OrderId uniqueidentifier, @ServiceName nvarchar(200), @ExpectedPriceUnit nvarchar(20), @VerifiedMeasurement decimal(12,3), @Amount decimal(12,2);
    SELECT @OrderId = q.order_id, @ServiceName = s.service_name_snapshot, @ExpectedPriceUnit = s.price_unit_snapshot, @VerifiedMeasurement = oi.verified_measurement
    FROM [ordering].price_quotes q JOIN [ordering].orders o ON o.id = q.order_id
    JOIN [ordering].order_items oi ON oi.order_id = o.id
    JOIN [ordering].order_item_services s ON s.order_item_id = oi.id
    WHERE q.id = @QuoteId AND o.store_id = @StoreId AND q.status = 'DRAFT'
      AND s.id = @OrderItemServiceId AND s.status <> 'CANCELLED' AND s.delete_date IS NULL;
    IF @OrderId IS NULL THROW 51051, 'Draft quote or matching service not found in store scope.', 1;
    IF @PriceUnit <> @ExpectedPriceUnit THROW 51052, 'Quote unit does not match the requested service.', 1;
    IF (@PriceUnit = 'PIECE' AND @Quantity <> 1) OR (@PriceUnit = 'M2' AND (@VerifiedMeasurement IS NULL OR @Quantity <> @VerifiedMeasurement))
        THROW 51061, 'Quote quantity must match the inspected measurement.', 1;
    SET @Amount = ROUND(@UnitPrice * @Quantity, 2);
    IF EXISTS (SELECT 1 FROM [ordering].price_quote_lines WHERE price_quote_id = @QuoteId AND order_item_service_id = @OrderItemServiceId)
        UPDATE [ordering].price_quote_lines SET unit_price = @UnitPrice, price_unit = @PriceUnit, quantity = @Quantity,
            amount = @Amount, explanation = @Explanation, update_date = SYSUTCDATETIME()
        WHERE price_quote_id = @QuoteId AND order_item_service_id = @OrderItemServiceId;
    ELSE
        INSERT [ordering].price_quote_lines (id, price_quote_id, order_item_service_id, service_name_snapshot, unit_price, price_unit, quantity, amount, explanation)
        VALUES (NEWID(), @QuoteId, @OrderItemServiceId, @ServiceName, @UnitPrice, @PriceUnit, @Quantity, @Amount, @Explanation);
    UPDATE [ordering].price_quotes SET subtotal = (SELECT SUM(amount) FROM [ordering].price_quote_lines WHERE price_quote_id = @QuoteId AND delete_date IS NULL),
        total_amount = (SELECT SUM(amount) FROM [ordering].price_quote_lines WHERE price_quote_id = @QuoteId AND delete_date IS NULL) + pickup_fee + delivery_fee,
        update_date = SYSUTCDATETIME()
    WHERE id = @QuoteId;
END;
GO
