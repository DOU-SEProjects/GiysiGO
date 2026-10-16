CREATE PROCEDURE [ordering].[spCreateQuoteRevision]
    @OrderId uniqueidentifier, @StoreId uniqueidentifier, @ActorUserId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @OldOrderStatus nvarchar(40);
    SELECT @OldOrderStatus = status FROM [ordering].orders WITH (UPDLOCK, HOLDLOCK)
    WHERE id = @OrderId AND store_id = @StoreId AND status IN ('INSPECTING','AWAITING_STORE_APPROVAL','AWAITING_CUSTOMER_APPROVAL') AND delete_date IS NULL;
    IF @OldOrderStatus IS NULL
        THROW 51030, 'Order is not eligible for a quote.', 1;
    IF EXISTS (SELECT 1 FROM [ordering].order_items WHERE order_id = @OrderId AND inspection_status = 'PENDING' AND delete_date IS NULL)
        THROW 51031, 'All order items must be inspected before quoting.', 1;
    IF EXISTS (SELECT 1 FROM [ordering].order_item_services ois JOIN [ordering].order_items oi ON oi.id = ois.order_item_id
               WHERE oi.order_id = @OrderId AND ois.price_unit_snapshot = 'M2' AND oi.verified_measurement IS NULL
                 AND ois.status <> 'CANCELLED' AND ois.delete_date IS NULL)
        THROW 51062, 'Square-meter services require an inspected measurement before quoting.', 1;
    DECLARE @Revision int = (SELECT ISNULL(MAX(revision), 0) + 1 FROM [ordering].price_quotes WITH (UPDLOCK, HOLDLOCK) WHERE order_id = @OrderId);
    UPDATE [ordering].price_quotes SET status = 'SUPERSEDED', update_date = SYSUTCDATETIME()
    WHERE order_id = @OrderId AND status IN ('DRAFT', 'PENDING_STORE_APPROVAL', 'PUBLISHED');
    IF @OldOrderStatus <> 'AWAITING_STORE_APPROVAL'
    BEGIN
        UPDATE [ordering].orders SET status = 'AWAITING_STORE_APPROVAL', version = version + 1, update_date = SYSUTCDATETIME() WHERE id = @OrderId;
        INSERT [ordering].order_status_history (id, order_id, from_status, to_status, actor_user_id, source)
        VALUES (NEWID(), @OrderId, @OldOrderStatus, 'AWAITING_STORE_APPROVAL', @ActorUserId, 'STORE');
    END;
    DECLARE @QuoteId uniqueidentifier = NEWID();
    DECLARE @Subtotal decimal(12,2) = (SELECT COALESCE(SUM(ROUND(ois.estimated_unit_price * CASE ois.price_unit_snapshot WHEN 'M2' THEN oi.verified_measurement ELSE 1 END, 2)), 0)
        FROM [ordering].order_item_services ois JOIN [ordering].order_items oi ON oi.id = ois.order_item_id
        WHERE oi.order_id = @OrderId AND ois.status <> 'CANCELLED' AND ois.delete_date IS NULL);
    INSERT [ordering].price_quotes (id, order_id, revision, status, subtotal, pickup_fee, delivery_fee, total_amount, currency)
    VALUES (@QuoteId, @OrderId, @Revision, 'DRAFT', @Subtotal, 0, 0, @Subtotal, 'TRY');
    INSERT [ordering].price_quote_lines (id, price_quote_id, order_item_service_id, service_name_snapshot, unit_price, price_unit, quantity, amount)
    SELECT NEWID(), @QuoteId, ois.id, ois.service_name_snapshot, ois.estimated_unit_price, ois.price_unit_snapshot,
           CASE ois.price_unit_snapshot WHEN 'M2' THEN oi.verified_measurement ELSE 1 END,
           ROUND(ois.estimated_unit_price * CASE ois.price_unit_snapshot WHEN 'M2' THEN oi.verified_measurement ELSE 1 END, 2)
    FROM [ordering].order_item_services ois JOIN [ordering].order_items oi ON oi.id = ois.order_item_id
    WHERE oi.order_id = @OrderId AND ois.status <> 'CANCELLED' AND ois.delete_date IS NULL;
    SELECT @QuoteId AS Id;
END;
GO
