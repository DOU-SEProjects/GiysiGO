CREATE PROCEDURE [ordering].[spApproveAndPublishQuote]
    @QuoteId uniqueidentifier, @StoreId uniqueidentifier, @ActorUserId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @OrderId uniqueidentifier;
    SELECT @OrderId = q.order_id FROM [ordering].price_quotes q WITH (UPDLOCK, HOLDLOCK)
    JOIN [ordering].orders o ON o.id = q.order_id
    WHERE q.id = @QuoteId AND q.status = 'PENDING_STORE_APPROVAL' AND o.store_id = @StoreId AND o.status = 'AWAITING_STORE_APPROVAL';
    IF @OrderId IS NULL THROW 51056, 'Quote is not pending approval in this store.', 1;
    IF NOT EXISTS (SELECT 1 FROM [ordering].price_quote_lines WHERE price_quote_id = @QuoteId AND delete_date IS NULL)
        THROW 51057, 'Quote has no active lines.', 1;
    UPDATE [ordering].price_quotes SET status = 'PUBLISHED', expires_at = DATEADD(day, 7, SYSUTCDATETIME()), update_date = SYSUTCDATETIME()
    WHERE id = @QuoteId AND status = 'PENDING_STORE_APPROVAL';
    UPDATE [ordering].orders SET status = 'AWAITING_CUSTOMER_APPROVAL', version = version + 1, update_date = SYSUTCDATETIME()
    WHERE id = @OrderId AND status = 'AWAITING_STORE_APPROVAL';
    IF @@ROWCOUNT <> 1 THROW 51058, 'Order status changed concurrently.', 1;
    INSERT [ordering].quote_approval_history (id, price_quote_id, action, actor_user_id)
    VALUES (NEWID(), @QuoteId, 'APPROVED', @ActorUserId), (NEWID(), @QuoteId, 'PUBLISHED', @ActorUserId);
    INSERT [ordering].order_status_history (id, order_id, from_status, to_status, actor_user_id, source)
    VALUES (NEWID(), @OrderId, 'AWAITING_STORE_APPROVAL', 'AWAITING_CUSTOMER_APPROVAL', @ActorUserId, 'STORE');
END;
GO
